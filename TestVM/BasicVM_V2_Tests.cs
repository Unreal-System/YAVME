using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V2;

namespace TestVM
{
    [TestFixture]
    public class BasicVM_V2_Tests
    {
        class IO : IIOPlugin
        {
            public ushort ValueW { get; set; }
            public byte ValueB { get; set; }

            public ushort GetWord()
            {
                return ValueW;
            }

            public void PutWord(ushort data)
            {
                ValueW = data;
            }

            public byte GetByte()
            {
                return ValueB;
            }

            public void PutByte(byte data)
            {
                ValueB = data;
            }
        }

        [Test]
        public void V2_UShortToBytesLayout()
        {
            var t = new UShortToBytesLayout();

            t.Value = 100;

            byte lowByte = (byte)(t.Value & 0xff);
            byte highByte = (byte)((t.Value >> 8) & 0xff);

            Assert.AreEqual(t.LowByte, lowByte);
            Assert.AreEqual(t.HighByte, highByte);

            t.Value = 2000;

            lowByte = (byte)(t.Value & 0xff);
            highByte = (byte)((t.Value >> 8) & 0xff);

            Assert.AreEqual(t.LowByte, lowByte);
            Assert.AreEqual(t.HighByte, highByte);

            t.Value = 16016;

            lowByte = (byte)(t.Value & 0xff);
            highByte = (byte)((t.Value >> 8) & 0xff);

            Assert.AreEqual(t.LowByte, lowByte);
            Assert.AreEqual(t.HighByte, highByte);
        }

        /// <summary>
        /// Test accessing the Flag register values.
        /// </summary>
        [Test]
        public void V2_FlagRegisters()
        {
            Executive.Init();

            var c = Executive.vm.GetFlagValue(FlagRegister.OF);

            Assert.AreEqual(c, false);

            Executive.vm.SetFlagValue(FlagRegister.OF, true);
            Executive.vm.SetFlagValue(FlagRegister.ZF, true);
            Executive.vm.SetFlagValue(FlagRegister.SF, true);

            c = Executive.vm.GetFlagValue(FlagRegister.OF);

            Assert.AreEqual(c, true);

            Executive.vm.SetFlagValue(FlagRegister.OF, false);

            c = Executive.vm.GetFlagValue(FlagRegister.OF);
            Assert.AreEqual(c, false);

            c = Executive.vm.GetFlagValue(FlagRegister.ZF);

            Assert.AreEqual(c, true);
        }

        [Test]
        public void V2_FlagRegistersWithBytes()
        {
            Executive.Init();

            var c = Executive.vm.GetFlagValueAsByte(FlagRegister.OF);

            Assert.AreEqual(c, 0);

            Executive.vm.SetFlagValueAsByte(FlagRegister.OF, 1);
            Executive.vm.SetFlagValueAsByte(FlagRegister.ZF, 1);
            Executive.vm.SetFlagValueAsByte(FlagRegister.SF, 1);

            c = Executive.vm.GetFlagValueAsByte(FlagRegister.OF);

            Assert.AreEqual(c, 1);

            Executive.vm.SetFlagValueAsByte(FlagRegister.OF, 0);

            c = Executive.vm.GetFlagValueAsByte(FlagRegister.OF);
            Assert.AreEqual(c, 0);

            c = Executive.vm.GetFlagValueAsByte(FlagRegister.ZF);

            Assert.AreEqual(c, 1);
        }

        /// <summary>
        /// Simplest program
        /// </summary>
        [Test]
        public void V2_VM_Simple()
        {
            var io = new IO { };

            var vmi = new VM(io);

            var program = new byte[] { 0, 255 };

            byte location = 0;
            foreach (byte value in program)
            {
                vmi.Poke(location++, value);
            }

            vmi.Run();
        }

        /// <summary>
        /// Simple test using the INC instruction.
        /// </summary>
        [Test]
        public void V2_VM_INC_byte()
        {
            var io = new IO { ValueB = 10 };

            var vmi = new VM(io);

            var program = new byte[]
            {
              (byte)Instruction.NOP,
              (byte)Instruction.INP, (byte)InstructionWidth.Byte,
              (byte)Instruction.INC, (byte)InstructionWidth.Byte,
              (byte)Instruction.INC, (byte)InstructionWidth.Byte,
              (byte)Instruction.OUT, (byte)InstructionWidth.Byte,
              (byte)Instruction.END
            };

            byte location = 0;
            foreach (byte value in program)
            {
                vmi.Poke(location++, value);
            }

            vmi.Run();

            Assert.AreEqual(io.GetByte(), 12);
        }

        [Test]
        public void V2_VM_INC_word()
        {
            var io = new IO { ValueW = 10 };

            var vmi = new VM(io);

            var program = new byte[]
            {
              (byte)Instruction.NOP,
              (byte)Instruction.INP, (byte)InstructionWidth.Word,
              (byte)Instruction.INC, (byte)InstructionWidth.Word,
              (byte)Instruction.INC, (byte)InstructionWidth.Word,
              (byte)Instruction.OUT, (byte)InstructionWidth.Word,
              (byte)Instruction.END
            };

            byte location = 0;
            foreach (byte value in program)
            {
                vmi.Poke(location++, value);
            }

            vmi.Run();

            Assert.AreEqual(io.GetWord(), 12);
        }
    }
}
