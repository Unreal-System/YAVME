using NUnit.Framework;
using System;
using V1;
using Moq;
using System.Linq;

namespace TestVM
{
    [TestFixture]
    public class BasicVM_V1_Tests
    {
        class IO : IIOPlugin
        {
            public byte Value { get; set; }

            public IO(byte initial)
            {
                Value = initial;
            }

            public byte Get()
            {
                return Value;
            }

            public void Put(byte data)
            {
                Value = data;
            }
        }

        /// <summary>
        /// Test accessing the Flag register values.
        /// </summary>
        [Test]
        public void V1_FlagRegisters()
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
        public void V1_FlagRegistersWithBytes()
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
        public void V1_VM_Simple()
        {
            var io = new IO(10);

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
        public void V1_VM_INC()
        {
            var io = new IO(10);

            var vmi = new VM(io);

            var program = new byte[] { 0, 1, 7, 7, 2, 255 };

            byte location = 0;
            foreach (byte value in program)
            {
                vmi.Poke(location++, value);
            }

            vmi.Run();

            Assert.AreEqual(io.Get(), 12);
        }

        /// <summary>
        /// Simple test using the DEC instruction.
        /// </summary>
        [Test]
        public void V1_VM_DEC()
        {
            var io = new IO(10);

            var vmi = new VM(io);

            var program = new byte[] { 0, 1, 8, 8, 2, 255 };

            byte location = 0;
            foreach (byte value in program)
            {
                vmi.Poke(location++, value);
            }

            vmi.Run();

            Assert.AreEqual(io.Get(), 8);
        }

        /// <summary>
        /// Test the ADD instruction.
        /// </summary>
        [Test]
        public void V1_VM_ADD()
        {
            var io = new IO(10);

            var vmi = new VM(io);

            var program = new byte[]
            {
                0,
                1,
                5, 10,                
                2,
                255
            };

            byte location = 0;
            foreach (byte value in program)
            {
                vmi.Poke(location++, value);
            }

            vmi.Run();

            Assert.AreEqual(io.Get(), 20);
        }

        /// <summary>
        /// Test the SUB instruction.
        /// </summary>
        [Test]
        public void V1_VM_SUB()
        {
            var io = new IO(10);

            var vmi = new VM(io);

            var program = new byte[]
            {
                0,
                1,
                6, 10,
                2,
                255
            };

            byte location = 0;
            foreach (byte value in program)
            {
                vmi.Poke(location++, value);
            }

            vmi.Run();

            Assert.AreEqual(io.Get(), 0);
        }

        /// <summary>
        /// Test the PUT instruction.
        /// </summary>
        [Test]
        public void V1_VM_PUT()
        {
            const byte ADDRESS = 50;
            const byte VALUE = 10;

            var io = new IO(VALUE);

            var vmi = new VM(io);

            var program = new byte[]
            {
                (byte)Instruction.NOP,
                (byte)Instruction.INP,
                (byte)Instruction.PUT, (byte)RegisterName.A, ADDRESS, //this should put value in A to #ADDRESS
                (byte)Instruction.END
            };

            byte location = 0;
            foreach (byte value in program)
            {
                vmi.Poke(location++, value);
            }

            vmi.Run();

            Assert.AreEqual(vmi.Memory[ADDRESS], VALUE);
        }

        /// <summary>
        /// Test the GET instruction.
        /// </summary>
        [Test]
        public void V1_VM_GET()
        {
            const byte ADDRESS = 50;
            const byte VALUE = 10;

            var io = new IO(VALUE);

            var vmi = new VM(io);

            var program = new byte[]
            {
                (byte)Instruction.NOP,
                (byte)Instruction.INP,
                (byte)Instruction.PUT, (byte)RegisterName.A, ADDRESS, //this should put value in A to #ADDRESS
                (byte)Instruction.GET, (byte)RegisterName.A, ADDRESS, //this should get value from #ADDRESS into A
                (byte)Instruction.OUT,
                (byte)Instruction.END
            };

            byte location = 0;
            foreach (byte value in program)
            {
                vmi.Poke(location++, value);
            }

            vmi.Run();

            Assert.AreEqual(vmi.Memory[ADDRESS], VALUE);
            Assert.AreEqual(io.Get(), VALUE);
        }

        /// <summary>
        /// Test the JMZ (JMP) instruction.
        /// </summary>
        [Test]
        public void V1_VM_JMZ()
        {
            const byte JMP_ADDRESS = 0x02;
            const byte JMZ_ADDRESS = 0x07;
            const byte VALUE = 10;

            var io = new IO(VALUE);

            var vmi = new VM(io);

            var program = new byte[]
            {
                /*00             */(byte)Instruction.NOP,
                /*01             */(byte)Instruction.INP,
                /*02 JMP_ADDRESS */(byte)Instruction.DEC,
                /*03             */(byte)Instruction.JMZ, JMZ_ADDRESS,
                /*05             */(byte)Instruction.JMP, JMP_ADDRESS,
                /*07 JMZ_ADDRESS */(byte)Instruction.OUT,
                /*08             */(byte)Instruction.END
            };

            byte location = 0;
            foreach (byte value in program)
            {
                vmi.Poke(location++, value);
            }

            vmi.Run();

            //check the ZF was set
            Assert.AreEqual(vmi.GetFlagValue(FlagRegister.ZF), true);
            Assert.AreEqual(io.Get(), 0);
        }

        /// <summary>
        /// Test the JMN (JMP) instruction.
        /// </summary>
        [Test]
        public void V1_VM_JMN()
        {
            const byte JMP_ADDRESS = 0x02;
            const byte JMN_ADDRESS = 0x07;
            const byte VALUE = 10;

            var io = new IO(VALUE);

            var vmi = new VM(io);

            var program = new byte[]
            {
                /*00             */(byte)Instruction.NOP,
                /*01             */(byte)Instruction.INP,
                /*02 JMP_ADDRESS */(byte)Instruction.DEC,
                /*03             */(byte)Instruction.JMN, JMN_ADDRESS,
                /*05             */(byte)Instruction.JMP, JMP_ADDRESS,
                /*07 JMN_ADDRESS */(byte)Instruction.OUT,
                /*08             */(byte)Instruction.END
            };

            byte location = 0;
            foreach (byte value in program)
            {
                vmi.Poke(location++, value);
            }

            vmi.Run();

            //check the SF was set
            Assert.AreEqual(vmi.GetFlagValue(FlagRegister.SF), true);
            Assert.AreEqual(io.Get(), 255); //this is the unsigned 2's compliment value in decimal
            Assert.AreEqual((sbyte)(io.Get()), -1);
        }
    }
}
