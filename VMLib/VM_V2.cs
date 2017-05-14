using System;
using System.Runtime.InteropServices;

namespace V2
{
    /// <summary>
    /// This is a little VM experiment. 
    /// 
    /// This version uses a VM with Registers, but no stack.
    /// 
    /// This version adds in 16 addressing.
    /// </summary>
	public class VM
    {
        public VM(IIOPlugin io, ushort size = 255)
        {
            Memory = new byte[size];
            var registerSize = EnumHelper.Max(RegisterName.PC);
            Registers = new byte[registerSize];
            IO = io;
        }

        public byte[] Memory { get; set; }
        public byte[] Registers { get; set; }
        public IIOPlugin IO { get; set; }

        const byte TRUE = 1;
        const byte FALSE = 0;

        /// <summary>
		/// Poke the specified address and value.
		/// </summary>
		public void Poke(ushort address, byte value)
        {
            Memory[address] = value;
        }

        /// <summary>
        /// Peek the specified address.
        /// </summary>
        public byte PeekAsByte(ushort address)
        {
            return Memory[address];
        }

        /// <summary>
        /// Poke the specified address and value.
        /// </summary>
        public void Poke(ushort address, ushort value)
        {
            var t = new UShortToBytesLayout { Value = value };

            Poke(address, t.HighByte);
            Poke((ushort)(address + 1), t.LowByte);
        }

        /// <summary>
        /// Peek the specified address.
        /// </summary>
        public ushort Peek(ushort address)
        {
            var t = new UShortToBytesLayout { HighByte = Memory[address], LowByte = Memory[address + 1] };
            return t.Value;
        }

        /// <summary>
        /// Gets the next value as an instruction.
        /// </summary>
        public Instruction GetInstruction()
        {
            var instruction = GetNextValue();
            return (Instruction)instruction;
        }

        /// <summary>
        /// Converts a register name to an offset.
        /// </summary>
		byte ToRegisterOffset(RegisterName r)
        {
            var result = ((byte)r) - 1;
            return (byte)result;
        }

        /// <summary>
        /// Gets the register value.
        /// </summary>
        public byte GetRegisterValue(RegisterName r)
        {
            return Registers[ToRegisterOffset(r)];
        }

        /// <summary>
        /// Sets the register value.
        /// </summary>
        public void SetRegisterValue(RegisterName r, byte value)
        {
            Registers[ToRegisterOffset(r)] = value;
        }

        /// <summary>
        /// Gets the flag value.
        /// </summary>
        public byte GetFlagValueAsByte(FlagRegister f)
        {
            return GetFlagValue(f) ? TRUE : FALSE;
        }

        public bool GetFlagValue(FlagRegister f)
        {
            var value = (FlagRegister)GetRegisterValue(RegisterName.FL);
            return ((value & f) == f);
        }

        /// <summary>
        /// Sets the flag value.
        /// </summary>
        public void SetFlagValueAsByte(FlagRegister f, byte value)
        {
            SetFlagValue(f, value != FALSE);
        }

        /// <summary>
        /// Sets the flag value.
        /// </summary>
        public void SetFlagValue(FlagRegister f, bool value)
        {
            var oldValue = (FlagRegister)GetRegisterValue(RegisterName.FL);
            if (value)
            {
                var newValue = (byte)(oldValue | f);
                SetRegisterValue(RegisterName.FL, newValue);

            }
            else
            {
                var newValue = oldValue - f;
                SetRegisterValue(RegisterName.FL, newValue);
            }
        }

        public void ResetFlags()
        {
            SetRegisterValue(RegisterName.FL, 0);
        }

        /// <summary>
        /// Gets the pc.
        /// </summary>
        byte GetPC(bool increment = true)
        {
            var pc = GetRegisterValue(RegisterName.PC);
            if (increment)
            {
                SetRegisterValue(RegisterName.PC, (byte)(pc + 1));
            }
            return pc;
        }

        /// <summary>
        /// Gets the next value.
        /// </summary>
        byte GetNextValue()
        {
            var pc = GetPC();
            return Memory[pc];
        }

        /// <summary>
        /// puts the next value to the register specified
        /// </summary>
        void PutNextValueTo(RegisterName register)
        {
            SetRegisterValue(register, GetNextValue());
        }

        void SetBCRegisterValue(ushort value)
        {
            var t = new UShortToBytesLayout { Value = value };
            SetRegisterValue(RegisterName.B, t.HighByte);
            SetRegisterValue(RegisterName.C, t.LowByte);
        }

        ushort GetBCRegisterValue()
        {
            var t = new UShortToBytesLayout
            {
                HighByte = GetRegisterValue(RegisterName.B),
                LowByte = GetRegisterValue(RegisterName.C)
            };

            return t.Value;
        }

        void NOP() { }

        /// <summary>
        /// Inp reads the byte from the address in the next instruction
        /// and puts the value in to the A register
        /// </summary>
        void INP()
        {
            var width = (InstructionWidth)GetNextValue();
            switch (width)
            {
                case InstructionWidth.Byte:
                    var valueB = IO.GetByte();
                    SetRegisterValue(RegisterName.A, valueB);
                    break;

                case InstructionWidth.Word:
                    var valueW = IO.GetWord();
                    SetBCRegisterValue(valueW);                    
                    break;
            }
        }

        /// <summary>
        /// Writes value in A to the memory location in next instruction
        /// </summary>
        void OUT()
        {
            var width = (InstructionWidth)GetNextValue();
            switch (width)
            {
                case InstructionWidth.Byte:
                    var valueB = GetRegisterValue(RegisterName.A);
                    IO.PutByte(valueB);
                    break;

                case InstructionWidth.Word :
                    var valueW = GetBCRegisterValue();
                    IO.PutWord(valueW);
                    break;
            }
            
        }

        /// <summary>
        /// JuMPs to the address specified in next byte.
        /// Does this by setting the PC to the address  
        /// in the value read.
        /// </summary>
        void JMP()
        {
            var address = GetNextValue();
            SetRegisterValue(RegisterName.PC, address);
        }

        /// <summary>
		/// JuMps if Zero to the address specified in next byte.
		/// Does this by setting the PC to the address  
		/// in the value read.
		/// </summary>
		void JMZ()
        {
            //var address = GetNextValue();
            //var value = GetRegisterValue(RegisterName.A);
            //if (value == 0)
            //{
            //    SetRegisterValue(RegisterName.PC, address);
            //}

            var address = GetNextValue();
            var value = GetFlagValue(FlagRegister.ZF);
            if (value)
            {
                SetRegisterValue(RegisterName.PC, address);
            }
        }

        /// <summary>
		/// JuMps if Zero to the address specified in next byte.
		/// Does this by setting the PC to the address  
		/// in the value read.
		/// </summary>
		void JMN()
        {
            var address = GetNextValue();
            var value = GetFlagValue(FlagRegister.SF);
            if (value)
            {
                SetRegisterValue(RegisterName.PC, address);
            }
        }

        /// <summary>
        /// Increments the value in register A by 1
        /// </summary>
        void INC()
        {
            ResetFlags();
            var width = (InstructionWidth)GetNextValue();
            switch (width)
            {
                case InstructionWidth.Byte:
                    var valueB = GetRegisterValue(RegisterName.A);
                    var resultB = valueB + 1;
                    if (resultB > sbyte.MaxValue)
                    {
                        SetFlagValue(FlagRegister.OF, true);
                    }
                    SetRegisterValue(RegisterName.A, (byte)resultB);
                    break;

                case InstructionWidth.Word:
                    var valueW = GetBCRegisterValue();
                    var resultW = valueW + 1;
                    if (resultW > short.MaxValue)
                    {
                        SetFlagValue(FlagRegister.OF, true);
                    }
                    SetBCRegisterValue((ushort)resultW);
                    break;
            }
            
        }

        /// <summary>
        /// Decrements the value in register A by 1
        /// </summary>
        void DEC()
        {
            ResetFlags();
            var value = GetRegisterValue(RegisterName.A);
            var result = value - 1;
            if (result == 0)
            {
                SetFlagValue(FlagRegister.ZF, true);
            }
            if (result < 0)
            {
                SetFlagValue(FlagRegister.SF, true);
            }
            SetRegisterValue(RegisterName.A, (byte)result);
        }

        /// <summary>
        /// Takes next value, puts in B, then takes 
        /// value in A, adds value in B, stores
        /// the result in A.
        /// </summary>
        void ADD()
        {
            ResetFlags();
            PutNextValueTo(RegisterName.B);
            var result = (GetRegisterValue(RegisterName.A) + GetRegisterValue(RegisterName.B));
            if (result > sbyte.MaxValue)
            {
                SetFlagValue(FlagRegister.OF, true);
            }
            SetRegisterValue(RegisterName.A, (byte)result);
        }

        /// <summary>
        /// Takes next value, puts in B, then takes 
        /// value in A, subtracts value in B, stores
        /// the result in A.
        /// </summary>
        void SUB()
        {
            ResetFlags();
            PutNextValueTo(RegisterName.B);
            var result = GetRegisterValue(RegisterName.A) - GetRegisterValue(RegisterName.B);

            if (result == 0)
            {
                SetFlagValue(FlagRegister.ZF, true);
            }
            if (result < 0)
            {
                SetFlagValue(FlagRegister.SF, true);
            }
            SetRegisterValue(RegisterName.A, (byte)result);
        }

        /// <summary>
        /// Puts the value from memory location in to R1. 
        /// </summary>
        void GET()
        {
            var r1 = (RegisterName)GetNextValue();
            var location = GetNextValue();

            SetRegisterValue(r1, Memory[location]);
        }

        /// <summary>
        /// Takes a value from R1 and moves it to memory location        
        /// </summary>
        void PUT()
        {
            var r1 = (RegisterName)GetNextValue();
            var location = GetNextValue();

            Memory[location] = GetRegisterValue(r1);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Run()
        {
            var running = true;
            while (running)
            {
                var raw = GetNextValue();
                var instruction = (Instruction)raw;
                switch (instruction)
                {
                    case Instruction.NOP:
                        NOP();
                        continue;

                    case Instruction.INP:
                        INP();
                        break;

                    case Instruction.OUT:
                        OUT();
                        break;

                    case Instruction.INC:
                        INC();
                        break;

                    case Instruction.DEC:
                        DEC();
                        break;

                    case Instruction.ADD:
                        ADD();
                        break;

                    case Instruction.SUB:
                        SUB();
                        break;

                    case Instruction.GET:
                        GET();
                        break;

                    case Instruction.PUT:
                        PUT();
                        break;

                    case Instruction.JMP:
                        JMP();
                        break;

                    case Instruction.JMZ:
                        JMZ();
                        break;

                    case Instruction.JMN:
                        JMN();
                        break;

                    case Instruction.END:
                        running = false;
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Interface for making IO Plug-ins.
    /// </summary>
    public interface IIOPlugin
    {
        byte GetByte();
        void PutByte(byte data);

        ushort GetWord();
        void PutWord(ushort data);
    }

    /// <summary>
    /// Console IO Plug-in.
    /// </summary>
    public class ConsoleIOPlugin : IIOPlugin
    {
        public byte GetByte()
        {
            var input = Console.ReadLine();
            return byte.Parse(input);
        }

        public void PutByte(byte data)
        {
            Console.WriteLine(data);
        }

        public ushort GetWord()
        {
            var input = Console.ReadLine();
            return ushort.Parse(input);
        }

        public void PutWord(ushort data)
        {
            Console.WriteLine(data);
        }
    }

    /// <summary>
    /// Executive - simplified set up and run.
    /// </summary>
    public static class Executive
    {
        public static VM vm;

        public static void Init(ushort size = 0, IIOPlugin io = null)
        {
            if (size == 0)
            {
                vm = new VM(io ?? new ConsoleIOPlugin());
            }
            else
            {
                vm = new VM(io ?? new ConsoleIOPlugin(), size);
            }
        }

        public static void Run(byte[] program)
        {
            byte location = 0;
            foreach (byte value in program)
            {
                vm.Poke(location++, value);
            }

            vm.Run();
        }
    }

    /// <summary>
    /// Flag register.
    /// </summary>
    [Flags]
    public enum FlagRegister : byte
    {
        SF = 0x1, //sign flag : the negative flag or sign flag is a single bit in a system status (flag) register used to indicate whether the result of the last mathematical operation resulted in a value whose most significant bit was set. 
        ZF = 0x2, //Zero flag : the zero flag is used to check the result of an arithmetic operation, including bitwise logical instructions. It is set if an arithmetic result is zero, and reset otherwise
        OF = 0x4  //Overflow flag : is usually a single bit in a system status register used to indicate when an arithmetic overflow has occurred in an operation, indicating that the signed two's-complement result would not fit in the number of bits used for the operation
    }

    [StructLayout(LayoutKind.Explicit, Size = sizeof(ushort))]
    struct UShortToBytesLayout
    {
        [FieldOffset(0)]
        public ushort Value;
        [FieldOffset(0)]
        public byte LowByte;
        [FieldOffset(1)]
        public byte HighByte;
    }

    /// <summary>
    /// Register names.
    /// </summary>
    public enum RegisterName : byte
    {
        A = 1,
        B = 2,
        C = 3,
        D = 4,
        I = 6,
        X = 7,
        PC = 5,
        FL = 6,
    }

    /// <summary>
    /// Instruction we support.
    /// </summary>
    public enum Instruction : byte
    {
        NOP = 0x00,
        INP = 0x01,
        OUT = 0x02,
        PUT = 0x03,
        GET = 0x04,
        ADD = 0x05,
        SUB = 0x06,
        INC = 0x07,
        DEC = 0x08,
        JMP = 0x09,
        JMZ = 0x0A,
        JMN = 0x0B,
        END = 0xFF
    }

    public enum InstructionWidth : byte
    {
        Byte = 0x01,
        Word = 0x02
    }

    /// <summary>
    /// Enum helper. Simple little class to get mainly Max enum value.
    /// </summary>
    public static class EnumHelper
    {
        public static int Max(object enumInstance)
        {
            try
            {
                var fi = enumInstance.GetType().GetFields();
                var filedType = fi[0].GetValue(enumInstance).GetType();
                object o = Convert.ChangeType(fi[fi.Length - 1].GetValue(enumInstance), filedType);
                return Convert.ToInt32(o);
            }
            catch
            {
                return -1;
            }
        }
    }
}
