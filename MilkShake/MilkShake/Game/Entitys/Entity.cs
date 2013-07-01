﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using Milkshake.Tools.Extensions;

namespace Milkshake.Game.Entitys
{
    public class Entity
    {
        public int MaskSize { get; private set; }
        public BitArray Mask { get; private set; }
        public Hashtable UpdateData { get; private set; }
        public int UpdateCount { get; private set; }

        //public abstract int MaxDataLength;

        public Entity(int dataLength)
        {
            MaskSize = ((dataLength) + 32) / 32;
            Mask = new BitArray(dataLength, false);
            UpdateData = new Hashtable();
        }
        
        public void SetUpdateField<T>(int index, T value, byte offset = 0)
        {
            UpdateCount++;

            switch (value.GetType().Name)
            {
                case "SByte":
                case "Int16":
                    {
                        Mask.Set(index, true);

                        if (UpdateData.ContainsKey(index))
                            UpdateData[index] = (int)((int)UpdateData[index] | (int)((int)Convert.ChangeType(value, typeof(int)) << (offset * (value.GetType().Name == "Byte" ? 8 : 16))));
                        else
                            UpdateData[index] = (int)((int)Convert.ChangeType(value, typeof(int)) << (offset * (value.GetType().Name == "Byte" ? 8 : 16)));

                        break;
                    }
                case "Byte":
                case "UInt16":
                    {
                        Mask.Set(index, true);

                        if (UpdateData.ContainsKey(index))
                            UpdateData[index] = (uint)((uint)UpdateData[index] | (uint)((uint)Convert.ChangeType(value, typeof(uint)) << (offset * (value.GetType().Name == "Byte" ? 8 : 16))));
                        else
                            UpdateData[index] = (uint)((uint)Convert.ChangeType(value, typeof(uint)) << (offset * (value.GetType().Name == "Byte" ? 8 : 16)));

                        break;
                    }
                case "Int64":
                    {
                        Mask.Set(index, true);
                        Mask.Set(index + 1, true);

                        long tmpValue = (long)Convert.ChangeType(value, typeof(long));

                        UpdateData[index] = (uint)(tmpValue & Int32.MaxValue);
                        UpdateData[index + 1] = (uint)((tmpValue >> 32) & Int32.MaxValue);

                        break;
                    }
                case "UInt64":
                    {
                        Mask.Set(index, true);
                        Mask.Set(index + 1, true);

                        ulong tmpValue = (ulong)Convert.ChangeType(value, typeof(ulong));

                        UpdateData[index] = (uint)(tmpValue & UInt32.MaxValue);
                        UpdateData[index + 1] = (uint)((tmpValue >> 32) & UInt32.MaxValue);

                        break;
                    }
                default:
                    {
                        Mask.Set(index, true);
                        UpdateData[index] = value;

                        break;
                    }
            }
        }

        public void WriteUpdateFields(BinaryWriter packet)
        {
            packet.Write((byte)MaskSize);

            packet.WriteBitArray(Mask, (MaskSize * 4));    // Int32 = 4 Bytes

            for (int i = 0; i < Mask.Count; i++)
            {
                if (Mask.Get(i))
                {
                    try
                    {
                        switch (UpdateData[i].GetType().Name)
                        {
                            case "UInt32":
                                packet.Write((uint)UpdateData[i]);
                                break;
                            case "Single":
                                packet.Write((float)UpdateData[i]);
                                break;
                            default:
                                packet.Write((int)UpdateData[i]);
                                break;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            Mask.SetAll(false);
            UpdateCount = 0;
        }
    }
}