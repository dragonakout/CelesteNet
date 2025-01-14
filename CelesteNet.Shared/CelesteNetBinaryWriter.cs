﻿using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Celeste.Mod.CelesteNet {
    public class CelesteNetBinaryWriter : BinaryWriter {

        public readonly DataContext Data;

        public StringMap? Strings;

        protected long SizeDummyIndex;
        protected byte SizeDummySize;

        public CelesteNetBinaryWriter(DataContext ctx, StringMap? strings, Stream output)
            : base(output, CelesteNetUtils.UTF8NoBOM) {
            Data = ctx;
            Strings = strings;
        }

        public CelesteNetBinaryWriter(DataContext ctx, StringMap? strings, Stream output, bool leaveOpen)
            : base(output, CelesteNetUtils.UTF8NoBOM, leaveOpen) {
            Data = ctx;
            Strings = strings;
        }

        public virtual void WriteSizeDummy(byte size) {
            Flush();
            SizeDummyIndex = BaseStream.Position;

            if (size == 1) {
                SizeDummySize = 1;
                Write((byte) 0);

            } else if (size == 4) {
                SizeDummySize = 4;
                Write((uint) 0);

            } else {
                SizeDummySize = 2;
                Write((ushort) 0);
            }
        }

        public virtual void UpdateSizeDummy() {
            if (SizeDummySize == 0)
                return;

            Flush();
            long end = BaseStream.Position;
            long length = end - (SizeDummyIndex + SizeDummySize);

            BaseStream.Seek(SizeDummyIndex, SeekOrigin.Begin);

            if (SizeDummySize == 1) {
                if (length > byte.MaxValue)
                    length = byte.MaxValue;
                Write((byte) length);

            } else if (SizeDummySize == 4) {
                if (length > uint.MaxValue)
                    length = uint.MaxValue;
                Write((uint) length);

            } else {
                if (length > ushort.MaxValue)
                    length = ushort.MaxValue;
                Write((ushort) length);
            }

            Flush();
            BaseStream.Seek(end, SeekOrigin.Begin);
        }

        public new void Write7BitEncodedInt(int value)
            => base.Write7BitEncodedInt(value);

        public virtual void Write(Vector2 value) {
            Write(value.X);
            Write(value.Y);
        }

        public virtual void Write(Color value) {
            Write(value.R);
            Write(value.G);
            Write(value.B);
            Write(value.A);
        }

        public virtual void WriteNoA(Color value) {
            Write(value.R);
            Write(value.G);
            Write(value.B);
        }

        public virtual void Write(DateTime value) {
            Write(value.ToBinary());
        }

        public virtual unsafe void WriteNetString(string? text) {
            if (text != null) {
                if (text.Length > 4096)
                    throw new Exception("String too long.");
                fixed (char* src = text) {
                    char* ptr = src;
                    for (int i = text.Length; i > 0; i--) {
                        Write(*ptr++);
                    }
                }
            }
            Write('\0');
        }

        public virtual void WriteNetMappedString(string? text) {
            if (Strings == null || !Strings.TryMap(text, out int id)) {
                WriteNetString(text);
                return;
            }

            Write((byte) 0xFF);
            Write7BitEncodedInt(id);
        }

        public void WriteRef<T>(T? data) where T : DataType<T>
            => Write((data ?? throw new Exception($"Expected {Data.DataTypeToID[typeof(T)]} to write, got null")).Get<MetaRef>(Data) ?? uint.MaxValue);

        public void WriteOptRef<T>(T? data) where T : DataType<T>
            => Write(data?.GetOpt<MetaRef>(Data) ?? uint.MaxValue);

    }
}
