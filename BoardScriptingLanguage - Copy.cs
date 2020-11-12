using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.WebUI;

namespace CWDM_Control_Board_GUI
{
	class BoardScriptingLanguage
	{		public const int ASSIGNMENT = 0;
			public const int PLUS = 1;
			public const int MINUS = 2;
			public const int MULTIPLICATION = 3;
			public const int DIVISION = 4;
			public const int RIGHT_BIT_SHIFT = 5;
			public const int LEFT_BIT_SHIFT = 6;
			public const int NOT = 7;
			public const int XOR = 8;
			public const int INCREMENT = 9;
			public const int DECREMENT = 10;
			public const int CONDITIONAL = 11;
		Dictionary<string, Variable> vars;
		public BoardScriptingLanguage(string[] program)
		{			
			vars = new Dictionary<string, Variable>();
			Variable xvars = new Int32(50);
			vars.Add("x",xvars);
			Variable x;
			vars.TryGetValue("x", out x);
			Console.WriteLine(((Int32)x).value);
		}
		abstract class Expression
		{

		}
		class Delay : Expression
		{

			public Delay(int milliseconds)
			{
				System.Threading.Thread.Sleep(milliseconds);
			}

			public Delay(Int intVal)
			{
				System.Threading.Thread.Sleep(((Int32)intVal).value);
			}

		}
		class Assignment : Expression
		{

		}
		abstract class Variable : Expression
		{
			public abstract Expression Not();
			public abstract Expression Increment();
			public abstract Expression Decrement();
			public abstract Expression Or(Variable var);
			public abstract Expression Xor(Variable var);
			public abstract Expression And(Variable var);

			public abstract bool Equals(Variable var);
			public abstract bool NotEquals(Variable var);
			public abstract Expression Add(Variable var);
			public abstract Expression Subtract(Variable var);

			public abstract Expression Multiply(Variable var);
			public abstract Expression Divide(Variable var);
			public abstract Expression RightBitShift(Variable var);
			public abstract Expression LeftBitShift(Variable var);

		}
		class String: Variable
		{
			public string value { get; set; }
			public String()
			{
				value = "";
			}
			public String(string val)
			{
				value = val;
			}

			public override Expression Not()
			{
				throw new InvalidOperationException("String Type Has No Not Operation");
			}

			public override Expression Increment()
			{
				throw new InvalidOperationException("String Type Has No Increment Operation");
			}

			public override Expression Decrement()
			{
				throw new InvalidOperationException("String Type Has No Decrement Operation");
			}

			public override Expression Or(Variable var)
			{
				throw new InvalidOperationException("String Type Has No Or Operation");
			}

			public override Expression And(Variable var)
			{
				throw new InvalidOperationException("String Type Has No And Operation");
			}

			public override Expression Xor(Variable var)
			{
				throw new InvalidOperationException("String Type Has No Xor Operation");
			}

			public override bool Equals(Variable var)
			{
				if(var is String)
				{
					return value.Equals(((String)var).value);
				}
				return false;
			}
			public override bool NotEquals(Variable var)
			{
				if (var is String)
				{
					
					return !value.Equals(((String)var).value);
				}
				return true;
			}

			public override Expression Add(Variable var)
			{
				return new String(value + ((String)var).value);
			}

			public override Expression Subtract(Variable var)
			{
				throw new InvalidOperationException("String Type Has No Subtract Operation");
			}

			public override Expression Multiply(Variable var)
			{
				throw new InvalidOperationException("String Type Has No Multiply Operation");
			}
			public override Expression Divide(Variable var)
			{
				throw new InvalidOperationException("String Type Has No Divide Operation");
			}
			public override Expression RightBitShift(Variable var)
			{
				throw new InvalidOperationException("String Type Has No Right Bit Shift Operation");
			}
			public override Expression LeftBitShift(Variable var)
			{
				throw new InvalidOperationException("String Type Has No Left Bit Shift Operation");
			}



		}
		class Bool : Variable
		{
			public bool value { get; set; }

			public Bool()
			{
				value = false;
			}
			public Bool(bool val)
			{
				value = val;
			}

			public override Expression Not()
			{
				return new Bool(!value);
			}
			/* The behavior of the boolean will be like a 1 bit number 
			 * So increment and decrement functions will always just flip the boolean
			 */
			public override Expression Increment()
			{
				value = !value;
				return this;
			}

			public override Expression Decrement()
			{
				value = !value;
				return this;
			}

			public override Expression Or(Variable var)
			{
				if (var is Bool)
				{
					return new Bool(value || ((Bool)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Boolean");
				}
			}

			public override Expression And(Variable var)
			{
				if (var is Bool)
				{
					return new Bool(value && ((Bool)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Boolean");
				}
			}

			public override bool Equals(Variable var)
			{
				if (var is Bool)
				{
					return value == ((Bool)var).value;
				}
				else
				{
					return false;
				}
			}

			public override bool NotEquals(Variable var)
			{
				if (var is Bool)
				{
					return value != ((Bool)var).value;
				}
				else
				{
					return true;
				}
			}

			public override Expression Xor(Variable var)
			{
				if (var is Bool)
				{
					return new Bool(value ^ ((Bool)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Boolean");
				}
			}

			public override Expression Add(Variable var)
			{
				if(var is String)
				{
					return new String(value ? "true" : "false").Add(var);
				}
				else 
				{ 
					throw new InvalidOperationException("Cannot Add Boolean Type");
				}
			}

			public override Expression Subtract(Variable var)
			{
				throw new InvalidOperationException("Cannot Subtract Boolean Type");
			}

			public override Expression Multiply(Variable var)
			{
				throw new InvalidOperationException("Cannot Multiply Boolean Type");
			}

			public override Expression Divide(Variable var)
			{
				throw new InvalidOperationException("Cannot Divide Boolean Type");
			}

			public override Expression RightBitShift(Variable var)
			{
				throw new InvalidOperationException("Cannot Right Shift Boolean Type");
			}

			public override Expression LeftBitShift(Variable var)
			{
				throw new InvalidOperationException("Cannot Left Shift Boolean Type");
			}


		}
		abstract class Number : Variable
		{

		}
		class Float : Number
		{
			public float value { get; set; }

			public Float()
			{
				value = 0;
			}
			public Float(float val)
			{
				value = val;
			}

			public static explicit operator Float(Int16 val)
			{
				return new Float(val.value);
			}
			public static explicit operator Float(Int32 val)
			{
				return new Float(val.value);
			}
			public static explicit operator Float(Int8 val)
			{
				return new Float(val.value);
			}


			public static explicit operator Float(UnsignedInt16 val)
			{
				return new Float(val.value);
			}
			public static explicit operator Float(UnsignedInt32 val)
			{
				return new Float(val.value);
			}
			public static explicit operator Float(UnsignedInt64 val)
			{
				return new Float(val.value);
			}
			public override Expression Not()
			{
				throw new InvalidOperationException("! Operation Cannot be Performed on Float");
			}
			public override Expression Increment()
			{
				value++;
				return this;
			}

			public override Expression Decrement()
			{
				value--;
				return this;
			}

			public override Expression Or(Variable var)
			{
				throw new InvalidOperationException("Or Operation Cannot be Performed on Float");
			}

			public override Expression And(Variable var)
			{
				throw new InvalidOperationException("And Operation Cannot be Performed on Float");
			}

			public override bool Equals(Variable var)
			{
				if (var is Number)
				{
					return value == ((Float)var).value;
				}
				else
				{
					return false;
				}
			}

			public override bool NotEquals(Variable var)
			{
				if (var is Number)
				{
					return value != ((Float)var).value;
				}
				else
				{
					return true;
				}
			}

			public override Expression Xor(Variable var)
			{
				throw new InvalidOperationException("Xor Operation Cannot be Performed on Float");
			}

			public override Expression Add(Variable var)
			{
				if (var is Number)
				{
					return new Float(value + ((Float)var).value);
				}
				else if(var is Bool)
				{
					return new Float(value + (((Bool)var).value?1:0));
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Subtract(Variable var)
			{
				if (var is Number)
				{
					return new Float( value - ((Float)var).value);
				}
				else if (var is Bool)
				{
					return new Float(((Bool)var).value ? value-1 : value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Multiply(Variable var)
			{
				if (var is Number)
				{
					return new Float(value * ((Float)var).value);
				}
				else if (var is Bool)
				{
					return new Float(((Bool)var).value ? value: 0);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Divide(Variable var)
			{
				if (var is Number)
				{
					float f = ((Float)var).value;
					if(f == 0)
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
					return new Float(value / f);
				}
				else if (var is Bool)
				{
					if (((Bool)var).value)
					{
						return new Float(value);
					}
					else
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression RightBitShift(Variable var)
			{
				throw new InvalidOperationException("Right Bit Shift Cannot be Performed on Float");
			}

			public override Expression LeftBitShift(Variable var)
			{
				throw new InvalidOperationException("Left Bit Shift Cannot be Performed on Float");
			}
		}
		abstract class Int : Number
		{

		}
		class Int64 : Int
		{
			public long value { get; set; }

			public Int64()
			{
				value = 0;
			}
			public Int64(long val)
			{
				value = val;
			}
			public static explicit operator Int64(Float val)
			{
				return new Int64((long)val.value);
			}

			public static explicit operator Int64(Int16 val)
			{
				return new Int64(val.value);
			}
			public static explicit operator Int64(Int32 val)
			{
				return new Int64(val.value);
			}
			public static explicit operator Int64(Int8 val)
			{
				return new Int64(val.value);
			}


			public static explicit operator Int64(UnsignedInt16 val)
			{
				return new Int64(val.value);
			}
			public static explicit operator Int64(UnsignedInt32 val)
			{
				return new Int64(val.value);
			}
			public static explicit operator Int64(UnsignedInt64 val)
			{
				return new Int64((long)val.value);
			}
			public override Expression Not()
			{
				return new Int64(~value);
			}
			public override Expression Increment()
			{
				value++;
				return this;
			}

			public override Expression Decrement()
			{
				value--;
				return this;
			}

			public override Expression Or(Variable var)
			{
				if (var is Int)
				{
					return new Int64(value | ((Int64)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression And(Variable var)
			{
				if (var is Int)
				{
					return new Int64(value & ((Int64)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override bool Equals(Variable var)
			{
				if (var is Int)
				{
					return value == ((Int64)var).value;
				}
				else
				{
					return false;
				}
			}

			public override bool NotEquals(Variable var)
			{
				if (var is Int)
				{
					return value != ((Int64)var).value;
				}
				else
				{
					return true;
				}
			}

			public override Expression Xor(Variable var)
			{
				if (var is Int)
				{
					return new Int64(value ^ ((Int64)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression Add(Variable var)
			{
				if (var is Number)
				{
					return new Int64(value + ((Int64)var).value);
				}
				else if (var is Bool)
				{
					return new Int64(((Bool)var).value ? value+1 : value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Subtract(Variable var)
			{

				if (var is Number)
				{
					return new Int64(value - ((Int64)var).value);
				}
				else if (var is Bool)
				{
					return new Int64(((Bool)var).value ? value-1 : value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Multiply(Variable var)
			{
				if (var is Number)
				{
					return new Int64(value * ((Int64)var).value);
				}
				else if (var is Bool)
				{
					return new Int64(((Bool)var).value ? value : 0);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Divide(Variable var)
			{
				if (var is Number)
				{
					long f = ((Int64)var).value;
					if (f == 0)
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
					return new Int64(value / f);
				}
				else if (var is Bool)
				{
					if (((Bool)var).value)
					{
						return new Int64(value);
					}
					else
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression RightBitShift(Variable var)
			{
				if (var is Int)
				{
					return new Int64(value >> ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression LeftBitShift(Variable var)
			{
				if (var is Int)
				{
					return new Int64(value << ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}
		}


		class Int32 : Int
		{
			public int value { get; set; }

			public Int32()
			{
				value = 0;
			}
			public Int32(int val)
			{
				value = val;
			}
			public static explicit operator Int32(Float val)
			{
				return new Int32((int)val.value);
			}
			public static explicit operator Int32(Int8 val)
			{
				return new Int32(val.value);
			}

			public static explicit operator Int32(Int16 val)
			{
				return new Int32(val.value);
			}

			public static explicit operator Int32(Int64 val)
			{
				return new Int32((int)val.value);
			}
			public static explicit operator Int32(UnsignedInt16  val)
			{
				return new Int32(val.value);
			}

			public static explicit operator Int32(UnsignedInt32 val)
			{
				return new Int32((int)val.value);
			}

			public static explicit operator Int32(UnsignedInt64 val)
			{
				return new Int32((int)val.value);
			}
			public override Expression Not()
			{
				return new Int32(~value);
			}
			public override Expression Increment()
			{
				value++;
				return this;
			}

			public override Expression Decrement()
			{
				value--;
				return this;
			}

			public override Expression Or(Variable var)
			{
				if (var is Int)
				{
					return new Int32(value | ((Int32)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression And(Variable var)
			{
				if (var is Int)
				{
					return new Int32(value & ((Int32)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override bool Equals(Variable var)
			{
				if (var is Int)
				{
					return value == ((Int32)var).value;
				}
				else
				{
					return false;
				}
			}

			public override bool NotEquals(Variable var)
			{
				if (var is Int)
				{
					return value != ((Int32)var).value;
				}
				else
				{
					return true;
				}
			}

			public override Expression Xor(Variable var)
			{
				if (var is Int)
				{
					return new Int32( value ^ ((Int32)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression Add(Variable var)
			{
				if (var is Number)
				{
					return new Int32(value + ((Int32)var).value);
				}
				else if (var is Bool)
				{
					return new Int32(((Bool)var).value ? value+1 : value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Subtract(Variable var)
			{

				if (var is Number)
				{
					return new Int32(value - ((Int32)var).value);
				}
				else if (var is Bool)
				{
					return new Int32(((Bool)var).value ? value-1 : value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}
			public override Expression Multiply(Variable var)
			{
				if (var is Number)
				{
					return new Int32(value * ((Int32)var).value);
				}
				else if (var is Bool)
				{
					return new Int32(((Bool)var).value ? value : 0);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Divide(Variable var)
			{
				if (var is Number)
				{
					int f = ((Int32)var).value;
					if (f == 0)
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
					return new Int32(value / f);
				}
				else if (var is Bool)
				{
					if (((Bool)var).value)
					{
						return new Int32(value);
					}
					else
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression RightBitShift(Variable var)
			{
				if (var is Int)
				{
					return new Int32(value >> ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression LeftBitShift(Variable var)
			{
				if (var is Int)
				{
					return new Int32(value << ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}
		}

		class Int16 : Int
		{
			public short value { get; set; }

			public Int16()
			{
				value = 0;
			}
			public Int16(short val)
			{
				value = val;
			}
			public Int16(int val)
			{
				value = (short)val;
			}
			public static explicit operator Int16(Float val)
			{
				return new Int16((short)val.value);
			}

			public static explicit operator Int16(Int64 val)
			{
				return new Int16((short)val.value);
			}
			public static explicit operator Int16(Int32 val)
			{
				return new Int16((short)val.value);
			}
			public static explicit operator Int16(Int8 val)
			{
				return new Int16(val.value);
			}

			public static explicit operator Int16(UnsignedInt64 val)
			{
				return new Int16((short)val.value);
			}
			public static explicit operator Int16(UnsignedInt32 val)
			{
				return new Int16((short)val.value);
			}
			public static explicit operator Int16(UnsignedInt16 val)
			{
				return new Int16((short)val.value);
			}
			public override Expression Not()
			{
				return new Int16( (short)~value);
			}
			public override Expression Increment()
			{
				value++;
				return this;
			}

			public override Expression Decrement()
			{
				value--;
				return this;
			}

			public override Expression Or(Variable var)
			{
				if (var is Int)
				{
					return new Int16(value | ((Int16)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression And(Variable var)
			{
				if (var is Int)
				{
					return new Int16(value & ((Int16)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override bool Equals(Variable var)
			{
				if (var is Int)
				{
					return value == ((Int16)var).value;
				}
				else
				{
					return false;
				}
			}

			public override bool NotEquals(Variable var)
			{
				if (var is Int)
				{
					return value != ((Int16)var).value;
				}
				else
				{
					return true;
				}
			}

			public override Expression Xor(Variable var)
			{
				if (var is Int)
				{
					return new Int16(value ^ ((Int16)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression Add(Variable var)
			{
				if (var is Number)
				{
					return new Int16(value + ((Int16)var).value);
				}
				else if (var is Bool)
				{
					return new Int16(((Bool)var).value ? value+1 : value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Subtract(Variable var)
			{
				if (var is Number)
				{
					return new Int16(value - ((Int16)var).value);
				}
				else if (var is Bool)
				{
					return new Int16(((Bool)var).value ? value - 1 : value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Multiply(Variable var)
			{
				if (var is Number)
				{
					return new Int16(value * ((Int16)var).value);
				}
				else if (var is Bool)
				{
					return new Int16(((Bool)var).value ? value : 0);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Divide(Variable var)
			{
				if (var is Number)
				{
					short f = ((Int16)var).value;
					if (f == 0)
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
					return new Int16(value / f);
				}
				else if (var is Bool)
				{
					if (((Bool)var).value)
					{
						return new Int16(value);
					}
					else
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression RightBitShift(Variable var)
			{
				if (var is Int)
				{
					return new Int16(value >> ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression LeftBitShift(Variable var)
			{
				if (var is Int)
				{
					return new Int16(value << ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}
		}

		class UnsignedInt64 : Int
		{
			public ulong value { get; set; }

			public UnsignedInt64()
			{
				value = 0;
			}
			public UnsignedInt64(ulong val)
			{
				value = val;
			}
			public static explicit operator UnsignedInt64(Float val)
			{
				return new UnsignedInt64((ulong)val.value);
			}
			public static explicit operator UnsignedInt64(Int16 val)
			{
				return new UnsignedInt64((ulong)val.value);
			}
			public static explicit operator UnsignedInt64(Int32 val)
			{
				return new UnsignedInt64((ulong)val.value);
			}
			public static explicit operator UnsignedInt64(Int8 val)
			{
				return new UnsignedInt64(val.value);
			}
			public static explicit operator UnsignedInt64(UnsignedInt16 val)
			{
				return new UnsignedInt64(val.value);
			}
			public static explicit operator UnsignedInt64(UnsignedInt32 val)
			{
				return new UnsignedInt64(val.value);
			}
			public static explicit operator UnsignedInt64(Int64 val)
			{
				return new UnsignedInt64((ulong)val.value);
			}
			public override Expression Not()
			{
				return new UnsignedInt64(~value);
			}
			public override Expression Increment()
			{
				value++;
				return this;
			}

			public override Expression Decrement()
			{
				value--;
				return this;
			}

			public override Expression Or(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt64(value | ((UnsignedInt64)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression And(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt64(value & ((UnsignedInt64)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override bool Equals(Variable var)
			{
				if (var is Int)
				{
					return value == ((UnsignedInt64)var).value;
				}
				else
				{
					return false;
				}
			}

			public override bool NotEquals(Variable var)
			{
				if (var is Int)
				{
					return value != ((UnsignedInt64)var).value;
				}
				else
				{
					return true;
				}
			}

			public override Expression Xor(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt64(value ^ ((UnsignedInt64)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression Add(Variable var)
			{
				if (var is Number)
				{
					return new UnsignedInt64(value + ((UnsignedInt64)var).value);
				}
				else if (var is Bool)
				{
					return new UnsignedInt64((((Bool)var).value ? value+1 : value));
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Subtract(Variable var)
			{
				if (var is Number)
				{
					return new UnsignedInt64(value - ((UnsignedInt64)var).value);
				}
				else if (var is Bool)
				{
					return new UnsignedInt64(((Bool)var).value ? value - 1 : value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Multiply(Variable var)
			{
				if (var is Number)
				{
					return new UnsignedInt64(value * ((UnsignedInt64)var).value);
				}
				else if (var is Bool)
				{
					return new UnsignedInt64(((Bool)var).value ? value : 0);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Divide(Variable var)
			{
				if (var is Number)
				{
					ulong f = ((UnsignedInt64)var).value;
					if (f == 0)
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
					return new UnsignedInt64(value / f);
				}
				else if (var is Bool)
				{
					if (((Bool)var).value)
					{
						return new UnsignedInt64(value);
					}
					else
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression RightBitShift(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt64(value >> ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression LeftBitShift(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt64(value << ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}
		}


		class UnsignedInt32 : Int
		{
			public uint value { get; set; }

			public UnsignedInt32()
			{
				value = 0;
			}
			public UnsignedInt32(uint val)
			{
				value = val;
			}
			public static explicit operator UnsignedInt32(Float val)
			{
				return new UnsignedInt32((uint)val.value);
			}
			public static explicit operator UnsignedInt32(Int8 val)
			{
				return new UnsignedInt32(val.value);
			}

			public static explicit operator UnsignedInt32(Int16 val)
			{
				return new UnsignedInt32((uint)val.value);
			}

			public static explicit operator UnsignedInt32(Int64 val)
			{
				return new UnsignedInt32((uint)val.value);
			}
			public static explicit operator UnsignedInt32(Int32 val)
			{
				return new UnsignedInt32((uint)val.value);
			}

			public static explicit operator UnsignedInt32(UnsignedInt16 val)
			{
				return new UnsignedInt32(val.value);
			}

			public static explicit operator UnsignedInt32(UnsignedInt64 val)
			{
				return new UnsignedInt32((uint)val.value);
			}

			public override Expression Not()
			{
				return new UnsignedInt32( ~value);
			}
			public override Expression Increment()
			{
				value++;
				return this;
			}

			public override Expression Decrement()
			{
				value--;
				return this;
			}

			public override Expression Or(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt32(value | ((UnsignedInt32)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression And(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt32(value & ((UnsignedInt32)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}


			public override bool Equals(Variable var)
			{
				if (var is Int)
				{
					return value == ((UnsignedInt32)var).value;
				}
				else
				{
					return false;
				}
			}

			public override bool NotEquals(Variable var)
			{
				if (var is Int)
				{
					return value != ((UnsignedInt32)var).value;
				}
				else
				{
					return true;
				}
			}

			public override Expression Xor(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt32(value ^ ((UnsignedInt32)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression Add(Variable var)
			{
				if (var is Number)
				{
					return new UnsignedInt32(value + ((UnsignedInt32)var).value);
				}
				else if (var is Bool)
				{
					return new UnsignedInt32((((Bool)var).value ? value+1 : value));
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Subtract(Variable var)
			{
				if(var is Number)
				{
					return new UnsignedInt32(value - ((UnsignedInt32)var).value);
				}
				else if (var is Bool)
				{
					return new UnsignedInt32(((Bool)var).value ? value - 1 : value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Multiply(Variable var)
			{
				if (var is Number)
				{
					return new UnsignedInt32(value * ((UnsignedInt32)var).value);
				}
				else if (var is Bool)
				{
					return new UnsignedInt32(((Bool)var).value ? value : 0);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Divide(Variable var)
			{
				if (var is Number)
				{
					uint f = ((UnsignedInt32)var).value;
					if (f == 0)
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
					return new UnsignedInt32(value / f);
				}
				else if (var is Bool)
				{
					if (((Bool)var).value)
					{
						return new UnsignedInt32(value);
					}
					else
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression RightBitShift(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt32(value >> ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression LeftBitShift(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt32(value << ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}
		}

		class UnsignedInt16 : Int
		{
			public ushort value { get; set; }

			public UnsignedInt16()
			{
				value = 0;
			}
			public UnsignedInt16(ushort val)
			{
				value = val;
			}
			public UnsignedInt16(int val)
			{
				value = (ushort)val;
			}
			public static explicit operator UnsignedInt16(Float val)
			{
				return new UnsignedInt16((ushort)val.value);
			}
			public static explicit operator UnsignedInt16(Int32 val)
			{
				return new UnsignedInt16((ushort)val.value);
			}
			public static explicit operator UnsignedInt16(Int8 val)
			{
				return new UnsignedInt16(val.value);
			}

			public static explicit operator UnsignedInt16(Int16 val)
			{
				return new UnsignedInt16((ushort)val.value);
			}

			public static explicit operator UnsignedInt16(Int64 val)
			{
				return new UnsignedInt16((ushort)val.value);
			}

			public static explicit operator UnsignedInt16(UnsignedInt32 val)
			{
				return new UnsignedInt16((ushort)val.value);
			}

			public static explicit operator UnsignedInt16(UnsignedInt64 val)
			{
				return new UnsignedInt16((ushort)val.value);
			}

			public override Expression Not()
			{
				return new UnsignedInt16((ushort)~value);
			}
			public override Expression Increment()
			{
				value++;
				return this;
			}

			public override Expression Decrement()
			{
				value--;
				return this;
			}

			public override Expression Or(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt16(value | ((UnsignedInt16)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression And(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt16(value & ((UnsignedInt16)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}


			public override bool Equals(Variable var)
			{
				if (var is Int)
				{
					return value == ((Int16)var).value;
				}
				else
				{
					return false;
				}
			}

			public override bool NotEquals(Variable var)
			{
				if (var is Int)
				{
					return value != ((Int16)var).value;
				}
				else
				{
					return true;
				}
			}

			public override Expression Xor(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt16(value ^ ((UnsignedInt16)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression Add(Variable var)
			{
				if (var is Number)
				{
					return new UnsignedInt32(value + ((UnsignedInt32)var).value);
				}
				else if (var is Bool)
				{
					return new UnsignedInt16((((Bool)var).value ? value+1 : value));
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Subtract(Variable var)
			{
				if (var is Number)
				{
					return new UnsignedInt16(value - ((UnsignedInt16)var).value);
				}
				else if (var is Bool)
				{
					return new UnsignedInt16(((Bool)var).value ? value - 1 : value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Multiply(Variable var)
			{
				if (var is Number)
				{
					return new UnsignedInt16(value * ((UnsignedInt16)var).value);
				}
				else if (var is Bool)
				{
					return new UnsignedInt16(((Bool)var).value ? value : 0);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression Divide(Variable var)
			{
				if (var is Number)
				{
					ushort f = ((UnsignedInt16)var).value;
					if (f == 0)
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
					return new UnsignedInt16(value / f);
				}
				else if (var is Bool)
				{
					if (((Bool)var).value)
					{
						return new UnsignedInt16(value);
					}
					else
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression RightBitShift(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt16(value >> ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression LeftBitShift(Variable var)
			{
				if (var is Int)
				{
					return new UnsignedInt16(value << ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}
		}

		class Int8 : Int
		{
			public byte value { get; set; }

			public Int8()
			{
				value = 0;
			}
			public Int8(byte val)
			{
				value = val;
			}
			public Int8(int val)
			{
				value = (byte)val;
			}
			public static explicit operator Int8(Float val)
			{
				return new Int8((byte)val.value);
			}
			public static explicit operator Int8(Int64 val)
			{
				return new Int8((byte)val.value);
			}
			public static explicit operator Int8(Int32 val)
			{
				return new Int8((byte)val.value);
			}

			public static explicit operator Int8(Int16 val)
			{
				return new Int8((byte)val.value);
			}
			public static explicit operator Int8(UnsignedInt64 val)
			{
				return new Int8((byte)val.value);
			}
			public static explicit operator Int8(UnsignedInt32 val)
			{
				return new Int8((byte)val.value);
			}

			public static explicit operator Int8(UnsignedInt16 val)
			{
				return new Int8((byte)val.value);
			}

			public override Expression Not()
			{
				return new Int8((byte)~value);
			}
			public override Expression Increment()
			{
				value++;
				return this;
			}

			public override Expression Decrement()
			{
				value--;
				return this;
			}

			public override Expression Or(Variable var)
			{
				if (var is Int)
				{
					return new Int8(value | ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Boolean");
				}
			}

			public override Expression And(Variable var)
			{
				if (var is Int)
				{
					return new Int8(value & ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Boolean");
				}
			}

			public override bool Equals(Variable var)
			{
				if (var is Int)
				{
					return value == ((Int8)var).value;
				}
				else
				{
					return false;
				}
			}

			public override bool NotEquals(Variable var)
			{
				if (var is Int)
				{
					return value != ((Int8)var).value;
				}
				else
				{
					return true;
				}
			}

			public override Expression Xor(Variable var)
			{
				if (var is Int)
				{
					return new Int8(value ^ ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression Add(Variable var)
			{
				if (var is Int)
				{
					return new Int8(value + ((Int8)var).value);
				}
				else if (var is Bool)
				{
					return new Int8((((Bool)var).value ? value+1 : value));
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression Subtract(Variable var)
			{
				if (var is Int)
				{
					return new Int8(value - ((Int8)var).value);
				}
				else if (var is Bool)
				{
					return new Int8((((Bool)var).value ? value - 1 : value));
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression Multiply(Variable var)
			{
				if (var is Number)
				{
					return new Int8(value * ((Int8)var).value);
				}
				else if (var is Bool)
				{
					return new Int8(((Bool)var).value ? value : 0);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression Divide(Variable var)
			{
				if (var is Number)
				{
					byte f = ((Int8)var).value;
					if (f == 0)
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
					return new Int8(value / f);
				}
				else if (var is Bool)
				{
					if (((Bool)var).value)
					{
						return new Int8(value);
					}
					else
					{
						throw new InvalidOperationException("Cannot Divide By Zero");
					}
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Numerical Type");
				}
			}

			public override Expression RightBitShift(Variable var)
			{
				if (var is Int)
				{
					return new Int8(value >> ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}

			public override Expression LeftBitShift(Variable var)
			{
				if (var is Int)
				{
					return new Int8(value << ((Int8)var).value);
				}
				else
				{
					throw new InvalidOperationException("Right Hand Expression Must Be Integer Type");
				}
			}
		}
	}
}
