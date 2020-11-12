using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Windows.UI.WebUI;

namespace CWDM_Control_Board_GUI
{
	class BoardScriptingLanguage
	{
		public Dictionary<string, object> vars;

		private HID_Connection connection;
		private MainWindow mainWindow;
		private string[] datatypes = new string[] { "int", "string", "double", "bool"};
		private List<string> methods = new List<string> { "Read", "Write", "Print","PrintLine", "Delay" };
		private List<(int, string)> parsedProgram;
		private Stack<List<string>> varStack;
		public BoardScriptingLanguage(HID_Connection tempConnection,MainWindow tempWindow,string[] program)
		{
			connection = tempConnection;
			mainWindow = tempWindow;
			Compile(program);
		}
		[Serializable]
		public class ProgramException : Exception
		{
			public int LineError { get; set; }
			public ProgramException() : base(){ }
			public ProgramException(string message) : base(message) { }
			public ProgramException(string message, int line) : this(message)
			{
				LineError = line;
			}
		}
		public class OperationException : Exception
		{
			public object value1 { get; set; }
			public object value2 { get; set; }
			public OperationException() : base() { }
			public OperationException(string message) : base(message) { }
			public OperationException(string message, object val1 = null, object val2 = null) : this(message)
			{
				value1 = val1;
				value2 = val2;
			}
		}
		private void Compile(string[] program)
		{
			vars = new Dictionary<string, object>();
			varStack = new Stack<List<string>>();
			List<(int, string)> example = tokenizeProgram(program);
			(int errorLine, bool valid) = verifyClosedProgram(example);
			parsedProgram = parseLines(example);
			runProgram(parsedProgram, true);
		}
		public void Run()
		{
			if(parsedProgram != null)
				runProgram(parsedProgram);
		}
		private List<(int, string)> tokenizeProgram(string[] program)
		{
			List<(int, string)> tokenizedProgram = new List<(int, string)>();
			int lineNum = 0;
			foreach (string line in program) {
				string[] processedLine = Regex.Split(line.TrimStart().TrimEnd(), @"([(),;{}=])");
				processedLine = processedLine.Where(x => !string.IsNullOrEmpty(x)).ToArray();
				if (processedLine.Length == 0) continue;
				if (processedLine[processedLine.Length - 1].Contains("}") || processedLine[processedLine.Length - 1].Contains("{") || processedLine[processedLine.Length - 1].Contains(";") || processedLine[processedLine.Length - 1].Contains(",")
					|| processedLine[0].Contains("if") || processedLine[0].Contains("for") || processedLine[0].Contains("while"))
				{ 
					foreach (string token in processedLine)
					{
						if(token.Contains("++") || token.Contains("--"))
						{
							string tokenCopy = token;
							while(tokenCopy.Contains("++") || tokenCopy.Contains("--"))
							{
								int plusplusindex = tokenCopy.IndexOf("++");
								int minusminusindex = tokenCopy.IndexOf("--");
								if((plusplusindex < minusminusindex || minusminusindex == -1) && plusplusindex != -1)
								{
									tokenizedProgram.Add((lineNum, tokenCopy.Substring(0, plusplusindex)));
									tokenizedProgram.Add((lineNum, tokenCopy.Substring(plusplusindex,2)));
									tokenCopy = tokenCopy.Substring(plusplusindex + 2);
									continue;
								}
								if ((plusplusindex > minusminusindex || plusplusindex == -1) && minusminusindex != -1)
								{
									tokenizedProgram.Add((lineNum, tokenCopy.Substring(0, minusminusindex)));
									tokenizedProgram.Add((lineNum, tokenCopy.Substring(minusminusindex,2)));
									tokenCopy = tokenCopy.Substring(minusminusindex + 2);
									continue;
								}
							}
							tokenizedProgram.Add((lineNum, tokenCopy));
							continue;
						}
						
						if (!token.Equals(""))
							tokenizedProgram.Add((lineNum, token));
					}
					lineNum++;
				}
				else
				{
					throw new ProgramException("Missing Terminating Character",lineNum);
				}

			}
			return tokenizedProgram;
		}
		public void runProgram(List<(int, string)> program,bool compile = false)
		{
			varStack.Push(new List<string>());
			bool breakFlag = false;
			for (int i = 0;i < program.Count;i++)
			{
				(int originalLineNumber, string line) = program[i];
				line = line.TrimStart(' ');
				string[] preprocessedLine = Regex.Split(line, @"([(),{}= ])");
				string[] processedLine = restitchQuotes(preprocessedLine);
				string inputs = "";

				if (processedLine[0].Equals("if"))
				{
					if (processedLine[1].Equals("("))
					{
						varStack.Push(new List<string>());
						Stack<string> stack = new Stack<string>();
						stack.Push(processedLine[1]);
						string evalString = "";
						int j;
						for(j = 2; j < processedLine.Length; j++)
						{

							if (processedLine[j].Equals(")"))
							{
								stack.Pop();
								if (stack.Count == 0)
									break;
								else
									evalString += processedLine[j];
							}
							else
							{
								if (processedLine[j].Equals("("))
								{
									stack.Push(processedLine[j]);
								}
								evalString += processedLine[j];
							}
						}
						bool evalBool;
						if (stack.Count == 0)
						{
							evalBool = (bool)evaluate(evalString,originalLineNumber,compile) && !breakFlag;
							if (!evalBool)
							{
								processedLine = processedLine.Skip(j + 3).ToArray();
								breakFlag = false;
							}
							else
								continue;
						}
					}
				}
				while (processedLine[0].Equals("") || processedLine[0].Equals(" "))
				{
					processedLine = processedLine.Skip(1).ToArray();
				}
				if (processedLine[0].Equals("else"))
					processedLine = processedLine.Skip(2).ToArray();
				if (processedLine[0].Equals("break") || processedLine[0].Equals("continue") || processedLine[0].Equals("goto"))
				{
					if (processedLine[0].Equals("break") || processedLine[0].Equals("continue"))
					{
						if (processedLine.Length < 5)
							throw new ProgramException("Invalid Syntax",originalLineNumber);
						if (processedLine[2].Equals("goto"))
						{
							if (compile)
							{
								int gotoLine;
								if (!int.TryParse(processedLine[4], out gotoLine))
								{
									throw new ProgramException("Cannot Parse Goto Line", originalLineNumber);
								}
							}
							else
							{
								i = Convert.ToInt32(processedLine[4], 10) - 1;
								if (processedLine[0].Equals("break"))
									breakFlag = true;
							}
						}

					}
					else if (processedLine[0].Equals("goto"))
					{
						if (processedLine.Length < 3)
							throw new ProgramException("Invalid Syntax", originalLineNumber);
						if (compile)
						{
							int gotoLine;
							if (!int.TryParse(processedLine[2], out gotoLine))
							{
								throw new ProgramException("Cannot Parse Goto Line", originalLineNumber);
							}
						}
						else
							i = Convert.ToInt32(processedLine[2], 10) - 1;
					}
					List<string> remove = varStack.Pop();
					foreach(string var in remove)
					{
						vars.Remove(var);
					}

				}
				else if (processedLine[0].Equals("int[]"))
				{
					List<int> array = new List<int>();
					if (processedLine[1].Equals("=")) throw new ProgramException("Variable Name Required",originalLineNumber);
					string varName = processedLine[2];
					if(processedLine.Length > 2 && processedLine[4].Equals("=") && processedLine[6].Equals("{"))
					{
						bool ended = false;
						for(int j = 7; j < processedLine.Length; j++)
						{
							
							if (processedLine[j].Equals("}"))
							{
								ended = true;
								break;
							}
							else if (processedLine[j].Equals(","))
							{
								continue;
							}
							else
							{
								array.Add((int)evaluate(processedLine[j],originalLineNumber,compile));
							}
						}
						if (!ended) throw new ProgramException("Array Requires Ending Brace",originalLineNumber);
						else
						{
							vars.Add(varName, array.ToArray());
							varStack.Peek().Add(varName);
						}
					}
					else
					{
						if(processedLine.Length > 8 && processedLine[4].Equals("=") && processedLine[6].Equals("new") && processedLine[8].Contains("int["))
						{
							int end = processedLine[8].IndexOf("]");
							if (end == -1) throw new ProgramException("Invalid Array Syntax",originalLineNumber);
							int arrayLength = int.Parse(processedLine[4].Substring(4, end));
							vars.Add(varName, new int[arrayLength]);
							varStack.Peek().Add(varName);
						}
						else
						{
							throw new ProgramException("Invalid Array Syntax",originalLineNumber);
						}
					}
				}
				else if (processedLine[0].Equals("string[]"))
				{
					List<string> array = new List<string>();
					if (processedLine[2].Equals("=")) throw new ProgramException("Variable Name Required",originalLineNumber);
					string varName = processedLine[2];
					if (processedLine.Length > 7 && processedLine[4].Equals("=") && processedLine[6].Equals("{"))
					{
						bool ended = false;
						for (int j = 7; j < processedLine.Length; j++)
						{

							if (processedLine[j].Equals("}"))
							{
								ended = true;
								break;
							}
							else if (processedLine[j].Equals(","))
							{
								continue;
							}
							else
							{
								array.Add((string)evaluate(processedLine[j],originalLineNumber,compile));
							}
						}
						if (!ended) throw new ProgramException("Array Requires Ending Brace",originalLineNumber);
						else
						{
							vars.Add(varName, array.ToArray());
							varStack.Peek().Add(varName);
						}
					}
					else
					{
						if (processedLine.Length > 8 && processedLine[4].Equals("=") && processedLine[6].Equals("new") && processedLine[8].Contains("string["))
						{
							int end = processedLine[8].IndexOf("]");
							if (end == -1) throw new ProgramException("Invalid Array Syntax",originalLineNumber);
							int arrayLength = int.Parse(processedLine[8].Substring(7, end));
							vars.Add(varName, new string[arrayLength]);
							varStack.Peek().Add(varName);
						}
						else
						{
							throw new ProgramException("Invalid Array Syntax",originalLineNumber);
						}
					}
				}
				else if (processedLine[0].Equals("double[]"))
				{
					List<double> array = new List<double>();
					if (processedLine[2].Equals("=")) throw new ProgramException("Variable Name Required",originalLineNumber);
					string varName = processedLine[2];
					if (processedLine.Length > 8 && processedLine[4].Equals("=") && processedLine[6].Equals("{"))
					{
						bool ended = false;
						for (int j = 7; j < processedLine.Length; j++)
						{

							if (processedLine[j].Equals("}"))
							{
								ended = true;
								break;
							}
							else if(processedLine[j].Equals(","))
							{
								continue;
							}
							else
							{
								array.Add((double)evaluate(processedLine[j],originalLineNumber,compile));
							}
						}
						if (!ended) throw new ProgramException("Array Requires Ending Brace",originalLineNumber);
						else
						{
							vars.Add(varName, array.ToArray());
							varStack.Peek().Add(varName);
						}
					}
					else
					{
						if (processedLine.Length > 7 && processedLine[4].Equals("=") && processedLine[6].Equals("new") && processedLine[8].Contains("double["))
						{
							int end = processedLine[8].IndexOf("]");
							if (end == -1) throw new ProgramException("Invalid Array Syntax",originalLineNumber);
							int arrayLength = int.Parse(processedLine[8].Substring(7, end));
							vars.Add(varName, new double[arrayLength]);
							varStack.Peek().Add(varName);
						}
						else
						{
							throw new ProgramException("Invalid Array Syntax",originalLineNumber);
						}
					}
				}
				else if (processedLine[0].Equals("bool[]"))
				{
					List<bool> array = new List<bool>();
					if (processedLine[2].Equals("=")) throw new ProgramException("Variable Name Required",originalLineNumber);
					string varName = processedLine[2];
					if (processedLine.Length > 7 && processedLine[4].Equals("=") && processedLine[6].Equals("{"))
					{
						bool ended = false;
						for (int j = 7; j < processedLine.Length; j++)
						{

							if (processedLine[j].Equals("}"))
							{
								ended = true;
								break;
							}
							else if (processedLine[j].Equals(","))
							{
								continue;
							}
							else
							{
								array.Add((bool)evaluate(processedLine[j],originalLineNumber,compile));
							}
						}
						if (!ended) throw new ProgramException("Array Requires Ending Brace",originalLineNumber);
						else
						{
							vars.Add(varName, array.ToArray());
							varStack.Peek().Add(varName);
						}
					}
					else
					{
						if (processedLine.Length > 7 && processedLine[4].Equals("=") && processedLine[6].Equals("new") && processedLine[8].Contains("bool["))
						{
							int end = processedLine[8].IndexOf("]");
							if (end == -1) throw new ProgramException("Invalid Array Syntax",originalLineNumber);
							int arrayLength = int.Parse(processedLine[8].Substring(5, end));
							vars.Add(varName, new bool[arrayLength]);
							varStack.Peek().Add(varName);
						}
						else
						{
							throw new ProgramException("Invalid Array Syntax", originalLineNumber);
						}
					}
				}
				else if (processedLine[0].Equals("int"))
				{
					if (processedLine[2].Equals("="))
					{
						throw new ProgramException("Variable Name Required",originalLineNumber);
					}
					else
					{
						string varName = processedLine[2];
						if (processedLine.Length > 5 && processedLine[4].Equals("="))
						{
							string assign = "";
							for (int j = 5; j < processedLine.Length; j++)
							{
								if (processedLine[j].Equals(","))
									break;
								else
									assign += processedLine[j];
							}
							object val = evaluate(assign,originalLineNumber,compile);
							if (val is int || val is double) { 
								vars.Add(varName, (int)val);
								varStack.Peek().Add(varName);
							}
						}
						else
						{
							vars.Add(varName, 0);
							varStack.Peek().Add(varName);
						}
					
					}
				}
				else if (line.StartsWith("string"))
				{
					if (processedLine[2].Equals("="))
					{
						throw new ProgramException("Variable Name Required",originalLineNumber);
					}
					else
					{
						string varName = processedLine[2];
						if (processedLine.Length > 5 && processedLine[4].Equals("="))
						{
							string assign = "";
							for (int j = 5; j < processedLine.Length; j++)
							{
								if (processedLine[j].Equals(","))
									break;
								else
									assign += processedLine[j];
							}
							object val = evaluate(assign,originalLineNumber,compile);
							if(val is string)
							{
								vars.Add(varName, (string)val);
								varStack.Peek().Add(varName);
							}
								
						}
						else
						{
							vars.Add(varName, "");
							varStack.Peek().Add(varName);
						}
					}
				}
				else if (line.StartsWith("double"))
				{
					if (processedLine[2].Equals("="))
					{
						throw new ProgramException("Variable Name Required",originalLineNumber);
					}
					else
					{
						string varName = processedLine[2];
						if (processedLine.Length > 2 && processedLine[4].Equals("="))
						{
							string assign = "";
							for (int j = 5; j < processedLine.Length; j++)
							{
								if (processedLine[j].Equals(","))
									break;
								else
									assign += processedLine[j];
							}
							object val = evaluate(assign,originalLineNumber,compile);
							if (val is int || val is double) { 
								vars.Add(varName, (double)val);
								varStack.Peek().Add(varName);
							}
						}
						else
						{
							vars.Add(varName, 0.0);
							varStack.Peek().Add(varName);
						}
					}
				}
				else if (line.StartsWith("bool"))
				{
					if (processedLine[2].Equals("="))
					{
						throw new ProgramException("Variable Name Required");
					}
					else
					{
						string varName = processedLine[2];
						if (processedLine.Length > 5 && processedLine[4].Equals("="))
						{
							string assign = "";
							for (int j = 5; j < processedLine.Length; j++)
							{
								if (processedLine[j].Equals(","))
									break;
								else
									assign += processedLine[j];
							}
							object val = evaluate(assign,originalLineNumber,compile);
							if (val is bool)
							{
								vars.Add(varName, (bool)val);
								varStack.Peek().Add(varName);
							}
						}
						else
						{
							vars.Add(varName, false);
							varStack.Peek().Add(varName);
						}
					}
				}
				else
				{
					object var;
					string varName = processedLine[0];
					if (!processedLine[2].Equals("=")){
						evaluate(line,originalLineNumber,compile);
						continue;
					}
					if (!vars.TryGetValue(varName, out var))
						throw new ProgramException("Variable Does Not Exist",originalLineNumber);
					if (processedLine.Length > 4)
					{
						string assign = "";
						for (int j = 4; j < processedLine.Length; j++)
						{
							if (processedLine[j].Equals(","))
								break;
							else
								assign += processedLine[j];
						}
						object val = evaluate(assign,originalLineNumber,compile);
						vars[varName] = val;
					}
				}
			}
			vars.Clear();
			varStack.Clear();
		}
		private string[] restitchQuotes(string[] line)
		{
			bool quote = false;
			string stitch = "";
			List<string> fixedLine = new List<string>();
			foreach(string elem in line)
			{
				if (quote)
				{
					if (elem.EndsWith("\""))
					{
						quote = false;
						stitch += elem;
						fixedLine.Add(stitch);
					}
					else
					{
						stitch += elem;
					}
				}
				else
				{
					if (elem.StartsWith("\"") && !elem.EndsWith("\""))
					{
						quote = true;
						stitch = elem;
					}
					else
					{
						if(!elem.Equals("") && !elem.Equals(""))
						fixedLine.Add(elem);
					}
				}
			}
			return fixedLine.ToArray();
		}




		public object evaluate(string evalLine,int originalLineNumber,bool compile = false)
		{
			Stack<object> stack = new Stack<object>();
			if (methods.ToArray().Any(evalLine.StartsWith))
			{
				string[] preprocessedLine = Regex.Split(evalLine, @"([(), ])");
				string[] processedLine = restitchQuotes(preprocessedLine);
				string inputs = "";
				if (processedLine[1] == "(")
				{
					List<string> expressions = new List<string>();
					Stack<string> parents = new Stack<string>();
					int tracker = processedLine[0].Length + processedLine[1].Length;
					for (int j = 2; j < processedLine.Length; j++)
					{
						tracker += processedLine[j].Length;
						if (methods.ToArray().Any(processedLine[j].Contains))
						{
							string subFunction = processedLine[j];
							Stack<string> subParents = new Stack<string>();
							j++;
							do
							{
								if (processedLine[j].Equals("("))
								{
									subParents.Push(processedLine[j]);
								}
								if (processedLine[j].Equals(")"))
								{
									subParents.Pop();
								}
								subFunction += processedLine[j];
								j++;
								tracker += processedLine[j].Length;
							} while (subParents.Count != 0);
							inputs += evaluate(subFunction, originalLineNumber, compile);
							j--;
						}
						else if (processedLine[j].Equals(","))
						{
							expressions.Add(inputs);
							inputs = "";
						}
						else if (processedLine[j].Equals(")"))
						{
							if (parents.Count == 0)
							{
								expressions.Add(inputs);
								inputs = "";
								evalLine = evalLine.Substring(tracker);
								break;
							}
							else
							{
								parents.Pop();
							}
						}
						else if (processedLine[j].Equals("("))
						{
							parents.Push(processedLine[j]);
						}
						else
							inputs += processedLine[j];
					}
					List<object> evaluatedParameters = new List<object>();
					foreach (string expr in expressions) evaluatedParameters.Add(evaluate(expr,originalLineNumber,compile));
					switch (processedLine[0])
					{
						case "Read":
							if(evaluatedParameters.Count > 0)
							{				
								if (evaluatedParameters[0] is string || evaluatedParameters[0] is bool)
									throw new ProgramException("Invalid Parameter Type",originalLineNumber);
								else if (compile)
								{
									stack.Push(0);
								}
								else
									stack.Push(Read((int)evaluatedParameters[0]));
							}
							break;
						case "Write":
							if (evaluatedParameters.Count > 0) { 
								if (evaluatedParameters[0] is string || evaluatedParameters[0] is bool || evaluatedParameters[1] is string || evaluatedParameters[1] is bool)
									throw new ProgramException("Invalid Parameter Type",originalLineNumber);
								else if (compile)
								{
									stack.Push(0);
								}
								else
									stack.Push(Write((int)evaluatedParameters[0],(int)evaluatedParameters[1]));
							}
							break;
						case "Delay":
							if (evaluatedParameters.Count > 0)
							{
								if (evaluatedParameters[0] is string || evaluatedParameters[0] is bool)
									throw new ProgramException("Invalid Parameter Type",originalLineNumber);
								if(!compile)
									Delay((int)evaluatedParameters[0]);
							}
							break;
						case "Print":
							if(evaluatedParameters.Count > 1)
							{
								if(evaluatedParameters[0] is string)
								{
									string statement = (string)evaluatedParameters[0];
									evaluatedParameters.RemoveAt(0);
									if(!compile)
										Print(statement, evaluatedParameters.ToArray());
								}
								else
								{
									throw new ProgramException("Invalid Parameter Format",originalLineNumber);
								}
							}
							else
							{
								if(!compile)
									Print(evaluatedParameters[0]);
							}
							break;
						case "PrintLine":
							if (evaluatedParameters.Count > 1)
							{
								if (evaluatedParameters[0] is string)
								{
									string statement = (string)evaluatedParameters[0];
									evaluatedParameters.RemoveAt(0);
									if(!compile)
										PrintLine(statement, evaluatedParameters.ToArray());
								}
							}
							else
							{
								if(!compile)
									PrintLine(evaluatedParameters[0]);
							}
							break;

					}

					//run each method here

				}
				else
				{
					throw new ProgramException("Invalid Syntax: Parenthesis Required!",originalLineNumber);
				}
			}

			if (evalLine.Equals(""))
			{
				if(stack.Count != 0)
				{
					return stack.Pop();
				}
				else
				{
					//void function
					return 0;
				}
			}


			//split the line into a list of tokens
			evalLine = evalLine.Replace("==", "#");
			evalLine = evalLine.Replace("!=", "~");
			evalLine = evalLine.Replace(">=", "@");
			evalLine = evalLine.Replace("<=", "$");

			List<string> infix = new List<string>(restitchQuotes(Regex.Split(evalLine, @"([()!=><+/*%#~$@ ])")));
			//This piece of code allows for spliting negative numbers and subtraction operation
			for(int i = 0; i < infix.Count; i++)
			{
				string operation = infix[i];
				if (isNumeric(operation) && operation.Substring(1).Contains("-"))
				{
					infix.RemoveAt(i);
					do
					{
						int index = operation.Substring(1).IndexOf("-");
						infix.Insert(i++, operation.Substring(0, index + 1));
						infix.Insert(i++, "-");
						operation = operation.Substring(index + 2);
					} while (isNumeric(operation) && operation.Substring(1).Contains("-"));
					infix.Insert(i++, operation);
				}
			}
			List<string> prefix = prefixConversion(infix);
			for(int i = prefix.Count -1 ; i >= 0; i--)
			{
				if (isOperator(prefix[i]))
				{
					object obj1 = stack.Pop();
					object obj2 = stack.Pop();
					try {
						switch (prefix[i]) {
							case "+":
								stack.Push(add(obj1, obj2));
								break;
							case "-":
								stack.Push(subtract(obj1, obj2));
								break;
							case "*":
								stack.Push(multiply(obj1, obj2));
								break;
							case "/":
								stack.Push(divide(obj1, obj2));
								break;
							case "^":
								stack.Push(power(obj1, obj2));
								break;
							case "#":
								stack.Push(EqualsCondition(obj1, obj2));
								break;
							case "~":
								stack.Push(NotEqualsCondition(obj1, obj2));
								break;
							case ">":
								stack.Push(GreaterThan(obj1, obj2));
								break;
							case "<":
								stack.Push(LessThan(obj1, obj2));
								break;
							case "@":
								stack.Push(GreaterThanOrEquals(obj1, obj2));
								break;
							case "$":
								stack.Push(LessThanOrEquals(obj1, obj2));
								break;
							case "`":
								stack.Push(add(obj1, obj2));
								break;
							case "_":
								stack.Push(subtract(obj1, obj2));
								break;
							default:
								throw new ProgramException("Error Invalid Operation!", originalLineNumber);
								//bogus error, will never be reached
						}
						
					}catch(OperationException ex)
					{
						throw new ProgramException(ex.Message, originalLineNumber);
					}
				}
				else
				{
					stack.Push(parseObject(prefix[i],originalLineNumber));
				}
			}
			//
			return stack.Pop();
		}

		public object parseObject(string value,int orignialLineNumber)
		{
			if (value.StartsWith("\"") && value.EndsWith("\""))
				return value.Substring(1, value.Length - 2);
			else if (value.Equals("true"))
				return true;
			else if (value.Equals("false"))
				return false;
			else if (isNumeric(value) && value.Contains("."))
			{
				return double.Parse(value, CultureInfo.InvariantCulture);
			}
			else if (isNumeric(value))
			{
				if (value.StartsWith("0x"))
					return Convert.ToInt32(value, 16);
				if (value.StartsWith("0b"))
					return Convert.ToInt32(value, 2);
				return int.Parse(value, CultureInfo.InvariantCulture);
			}
			else
			{
				if (value.Contains("["))
				{
					int idx = value.IndexOf("[");
					int idx2 = value.IndexOf("]");
					string arrayIndex = "";
					if (idx2 == -1) throw new ProgramException("Array Index Requires Closing Bracket",orignialLineNumber);
					else
					{
						arrayIndex = value.Substring(idx+1, idx2 - (idx+1));
					}
					string varName = value.Substring(0, idx);
					int parsedIndex = int.Parse(arrayIndex);
					object returnValue;
					bool success = vars.TryGetValue(varName, out returnValue);
					if (success)
					{
						if (returnValue is int[]) { 
							if (parsedIndex < 0 || parsedIndex >= ((int[])returnValue).Length)
								throw new ProgramException("Invalid Array Index",orignialLineNumber);
							return ((int[])returnValue)[parsedIndex];
						}
						else if (returnValue is double[])
						{
							if (parsedIndex < 0 || parsedIndex >= ((double[])returnValue).Length)
								throw new ProgramException("Invalid Array Index",orignialLineNumber);
							return ((double[])returnValue)[parsedIndex];
						}
						else if (returnValue is string[])
						{
							if (parsedIndex < 0 || parsedIndex >= ((string[])returnValue).Length)
								throw new ProgramException("Invalid Array Index",orignialLineNumber);
							return ((string[])returnValue)[parsedIndex];
						}
						else if(returnValue is bool[])
						{
							if (parsedIndex < 0 || parsedIndex >= ((bool[])returnValue).Length)
								throw new ProgramException("Invalid Array Index",orignialLineNumber);
							return ((bool[])returnValue)[parsedIndex];
						}
						else
						{
							throw new ProgramException("Invalid Array Datatype",orignialLineNumber);
						}

					}
					else
					{
						throw new ProgramException("Invalid Variable Name",orignialLineNumber);
					}
				}
				else { 
					object returnValue;
					bool success = vars.TryGetValue(value,out returnValue);
					if (success) return returnValue;
					else
					{
						throw new ProgramException("Invalid Variable Name", orignialLineNumber);
					}
				}
			}
		}

		private List<string> prefixConversion(List<string> infix)
		{
			//this method converts infix statements into prefix statements to avoid pemdas problems
			// stack for operators. 
			Stack<string> operators = new Stack<string>();
			// stack for operands. 
			Stack<string> operands = new Stack<string>();
			for (int i = 0; i < infix.Count; i++)
			{
				if (infix[i].Equals("") || infix[i].Equals(" ")) continue;
				// If current character is an 
				// opening bracket, then 
				// push into the operators stack. 
				if (infix[i].Equals("("))
				{
					operators.Push(infix[i]);
				}
				else if (infix[i].Equals(")"))
				{
					while ((operators.Count != 0) &&  !operators.Peek().Equals("("))
					{
						
						// operand 1 
						string op1 = operands.Pop();
						// operand 2 
						string op2 = operands.Pop();
						// operator 
						string op = operators.Pop();
						// Add operands and operator 
						// in form operator + 
						// operand1 + operand2. 
						string tmp = op+" " + op2+" " + op1;
						operands.Push(tmp);
					}// stack. 
					operators.Pop();
				}
				else if (!isOperator(infix[i]))
				{
					operands.Push(infix[i]);
				}
				else
				{
					while ((operators.Count!= 0) && (getPriority(infix[i]) <= getPriority(operators.Peek())))
					{
							string op1 = operands.Pop();

							string op2 = operands.Pop();

							string op = operators.Pop();

							string tmp = op + " " + op2 + " " + op1;
							operands.Push(tmp);
					}

						operators.Push(infix[i]);
					}
				}

				// Pop operators from operators stack 
				// until it is empty and add result 
				// of each pop operation in 
				// operands stack. 
				while (operators.Count != 0)
				{
					string op1 = operands.Pop();

					string op2 = operands.Pop();

					string op = operators.Pop();

					string tmp = op + " " + op2 + " " + op1;
					operands.Push(tmp);
				}

			// Final prefix expression is 
			// present in operands stack. 
			string result = operands.Pop();
			if (result.StartsWith("\"") && result.EndsWith("\""))
			{
				return new List<string> { result };
			}
			string[] processedLine = Regex.Split(result, @"([ ])");
			string[] processedLine2 = restitchQuotes(processedLine);
			return new List<string>(processedLine2).Where(x => !x.Equals(" ")).ToList(); ;
		}

		private bool isOperator(string c)
		{
				/*
				 * # is ==
				 * ~ = !=
				 * @ >=
				 * $ <=
				 * ` ++
				 * _ --
				 */
				return c.Equals("+") || c.Equals("-") || c.Equals("*")
				|| c.Equals("/") || c.Equals("#") || c.Equals("^")
				|| c.Equals("%") || c.Equals("~") || c.Equals("@")
				|| c.Equals("$") || c.Equals("!") || c.Equals(">")
				|| c.Equals("<") || c.Equals("`") || c.Equals("_");
		}

		private bool isNumeric(string c)
		{
			return c.StartsWith("0") || c.StartsWith("1") || c.StartsWith("2")
				|| c.StartsWith("3") || c.StartsWith("4") || c.StartsWith("5")
				|| c.StartsWith("6") || c.StartsWith("7") || c.StartsWith("8")
				|| c.StartsWith("9") || c.StartsWith("-");
		}

		private int getPriority(string c)
			{
			if (c.Equals("+") || c.Equals("-"))
				return 1;
			else if (c.Equals("*") || c.Equals("/"))
				return 2;
			else if (c.Equals("^") || c.Equals("`") || c.Equals("_"))
				return 3;
			else
				return 0;
			}

		private int Read(int register)
		{
			return connection.SendReadCommand((ushort)register);
		}

		private int Write(int register,int value)
		{
			return connection.SendWriteCommand((ushort)register, (ushort)value);
		}
		private void Delay(int milliseconds)
		{
			Thread.Sleep(milliseconds);
		}
		private void Print(object obj)
		{
			mainWindow.PrintOutput(obj.ToString());
		}
		private void Print(string printText, object[] printStatements)
		{
			mainWindow.PrintOutput(printText);
			foreach (object state in printStatements) mainWindow.PrintOutput(state.ToString());
		}

		private void PrintLine(object obj)
		{
			mainWindow.PrintLineOutput(obj.ToString());
		}
		private void PrintLine(string printText, object[] printStatements)
		{
			mainWindow.PrintLineOutput(printText);
			foreach (object state in printStatements) mainWindow.PrintLineOutput(state.ToString());
		}
		private List<(int, string)> parseLines(List<(int, string)> tokens, int startLine = 0, int breakLine = 0, string breakCommand = "", int correctionLine = 0)
		{
			List<(int, string)> parsedProgram = new List<(int, string)>();
			string parsedLine = "";
			for (int i = 0; i < tokens.Count; i++)
			{
				Queue<string> incr_decr_lines = new Queue<string>();
				(int programLine, string elem) = tokens[i];
				if (elem.Equals("for"))
				{
					//Determines the elongated code for a for loop
					//Adds a declaration line
					i++;
					(programLine, elem) = tokens[i];
					//start with interpreting the for loop
					if (elem.Equals("("))
					{
						List<(int, string)> declaration = new List<(int, string)>();
						//parse the initial declaration and add it to its own line
						while (!elem.Equals(";"))
						{
							i++;
							(programLine, elem) = tokens[i];
							declaration.Add((programLine, elem));
							if (elem.Equals(")"))
							{
								throw new ProgramException("Error Invalid Syntax", programLine);
							}
						}
						List<(int, string)> forDeclaration = parseLines( declaration, startLine, breakLine);
						foreach ((int blockNum, string blockLine) in forDeclaration)
						{
							parsedProgram.Add((blockNum, blockLine));
							startLine++;
						}
						//parse the condition statement
						string ifStatement = "if(";
						i++;
						(programLine, elem) = tokens[i];
						while (!elem.Equals(";"))
						{
							ifStatement += elem;
							i++;
							(programLine, elem) = tokens[i];
							if (elem.Equals(")"))
							{
								throw new ProgramException("Error Invalid Syntax", programLine);
							}
						}
						ifStatement += ") else goto ";
						i++;
						(programLine, elem) = tokens[i];
						string updateLine = "";
						List<(int, string)> updateList = new List<(int, string)>();
						while (!elem.Equals(")"))
						{
							updateList.Add((programLine, elem));
							i++;
							(programLine, elem) = tokens[i];
							if (elem.Equals(";"))
							{
								throw new ProgramException("Error Invalid Syntax" , programLine);
							}
						}
						updateList.Add((programLine, ";"));
						updateLine = parseLines(updateList, startLine, breakLine).ToArray()[0].Item2;
						i++;
						int forLine = programLine;
						(programLine, elem) = tokens[i];


						List<(int, string)>  sublist = new List<(int, string)>();
						if (elem.Equals("{"))
						{
							Stack<string> block = new Stack<string>();
							block.Push(elem);
							while (block.Count != 0)
							{
								i++;
								(int lineNum, string lineElem) = tokens[i];
								if (lineElem.Equals("{"))
								{
									block.Push(lineElem);
								}
								if (lineElem.Equals("}"))
								{
									block.Pop();
								}
								if (block.Count != 0)
									sublist.Add((lineNum, lineElem));
							}
						}
						else
						{

							sublist.Add((programLine, elem));
							do
							{
								i++;
								(programLine, elem) = tokens[i];
								sublist.Add((programLine, elem));
							} while (!elem.Equals(";"));
						}
						int parsedLineNum = startLine;
						List<(int, string)> parsedSubProgram = parseLines(sublist,startLine+1,startLine,updateLine,forLine);
						ifStatement += (startLine + parsedSubProgram.Count+3);
						parsedProgram.Add((forLine, ifStatement));
						startLine++;
						foreach ((int blockNum, string blockLine) in parsedSubProgram)
						{
							parsedProgram.Add((blockNum, blockLine));
							startLine++;
						}
						parsedProgram.Add((forLine, updateLine));
						startLine++;
						parsedProgram.Add((forLine, "goto " + parsedLineNum));
						startLine++;
						parsedLine = "";
					}	
					else
					{
						throw new ProgramException("Error Invalid Syntax: " + programLine);
					}
				}
				else if(elem.Equals("while"))
				{
					//Determines the elongated code for a while loop
					i++;
					(programLine, elem) = tokens[i];
					//start with interpreting the for loop
					if (elem.Equals("("))
					{
						//parse the condition
						
						string ifStatement = "if(";
						i++;
						(programLine, elem) = tokens[i];
						while (!elem.Equals(")"))
						{
							ifStatement += elem;
							i++;
							(programLine, elem) = tokens[i];
						}
						//if the condition is evaluated to be false it needs to know which line to goto
						ifStatement += ")else goto ";
						i++;
						(programLine, elem) = tokens[i];
						int whileLine = programLine;
						List<(int, string)> sublist = new List<(int, string)>();
						if (elem.Equals("{"))
						{
							Stack<string> block = new Stack<string>();
							block.Push(elem);
							while (block.Count != 0)
							{
								i++;
								(int lineNum, string lineElem) = tokens[i];
								if (lineElem.Equals("{"))
								{
									block.Push(lineElem);
								}
								if (lineElem.Equals("}"))
								{
									block.Pop();
								}
								if (block.Count != 0)
									sublist.Add((lineNum, lineElem));
							}
						}
						else
						{

							sublist.Add((programLine, elem));
							do
							{
								i++;
								(programLine, elem) = tokens[i];
								sublist.Add((programLine, elem));
							} while (!elem.Equals(";"));
						}
						int parsedLineNum = startLine;
						List<(int, string)> parsedSubProgram = parseLines(sublist, startLine + 1, startLine);
						ifStatement += (startLine + parsedSubProgram.Count + 2);
						parsedProgram.Add((whileLine, ifStatement));
						startLine++;
						foreach ((int blockNum, string blockLine) in parsedSubProgram)
						{
							parsedProgram.Add((blockNum, blockLine));
							startLine++;
						}
						parsedProgram.Add((whileLine, "goto " + parsedLineNum));
						startLine++;
						parsedLine = "";
					}
					else if (elem.Equals("}"))
					{
						continue;
					}
					else
					{
						throw new ProgramException("Error Invalid Syntax", programLine);
					}
				}
				else if(elem.Equals("if"))
				{
					i++;
					(programLine, elem) = tokens[i];
					if (elem.Equals("("))
					{
						int ifLine = programLine;
						parsedLine = "if";
						while (!elem.Equals(")"))
						{
							parsedLine += elem;
							i++;
							(programLine, elem) = tokens[i];
						}

						if (parsedLine.Equals("")) throw new ProgramException("Error Invalid Syntax", programLine);
						parsedLine += ") else goto ";
						string ifStatement = parsedLine;
						parsedLine = "";
						i++;
						(programLine, elem) = tokens[i];
						List<(int, string)> sublist = new List<(int, string)>();
						if(elem.Equals("{"))
						{ 
							Stack<string> block = new Stack<string>();
							block.Push(elem);
							while (block.Count != 0)
							{
								i++;
								(int lineNum, string lineElem) = tokens[i];
								if (lineElem.Equals("{"))
								{
									block.Push(lineElem);
								}
								if (lineElem.Equals("}"))
								{
									block.Pop();
								}
								if (block.Count != 0)
									sublist.Add((lineNum, lineElem));
							}
						}
						else
						{

							sublist.Add((programLine, elem));
							do
							{
								i++;
								(programLine, elem) = tokens[i];
								sublist.Add((programLine, elem));
							} while (!elem.Equals(";"));
						}
						int parsedLineNum = startLine;
						List<(int, string)> parsedIfSubProgram = parseLines(sublist, startLine + 1, breakLine,breakCommand,correctionLine);
						if (i < tokens.Count-1)
						{
							i++;
							(programLine, elem) = tokens[i];
						}
						else elem = "";
						if (elem.Equals("else"))
						{
							i++;
							(programLine, elem) = tokens[i];
							sublist = new List<(int, string)>();
							if (elem.Equals("{"))
							{
								Stack<string> block = new Stack<string>();
								block.Push(elem);
								while (block.Count != 0)
								{
									i++;
									(int lineNum, string lineElem) = tokens[i];
									if (lineElem.Equals("{"))
									{
										block.Push(lineElem);
									}
									if (lineElem.Equals("}"))
									{
										block.Pop();
									}
									if (block.Count != 0)
										sublist.Add((lineNum, lineElem));
								}
							}
							else
							{

								sublist.Add((programLine, elem));
								do
								{
									i++;
									(programLine, elem) = tokens[i];
									sublist.Add((programLine, elem));
								} while (!elem.Equals(";"));
							}
							int parsedLineNum2 = startLine;
							List<(int, string)> parsedElseSubProgram = parseLines(sublist, startLine+parsedIfSubProgram.Count+2, breakLine,breakCommand,correctionLine);
							ifStatement += (startLine + parsedIfSubProgram.Count+1);
							parsedProgram.Add((ifLine, ifStatement));
							startLine++;
							foreach ((int blockNum, string blockLine) in parsedIfSubProgram)
							{
								parsedProgram.Add((blockNum, blockLine));
								startLine++;
							}
							parsedProgram.Add((ifLine, "goto " + (startLine + parsedElseSubProgram.Count+2)));
							startLine++;
							foreach ((int blockNum, string blockLine) in parsedElseSubProgram)
							{
								parsedProgram.Add((blockNum, blockLine));
								startLine++;
							}
							parsedLine = "";
						}
						else
						{
							ifStatement += (startLine + parsedIfSubProgram.Count+1);
							parsedProgram.Add((ifLine, ifStatement));
							startLine++;
							foreach ((int blockNum, string blockLine) in parsedIfSubProgram)
							{
								parsedProgram.Add((blockNum, blockLine));
								startLine++;
							}
							parsedLine = "";
							i--;
						}
					}
					else
					{
						throw new ProgramException("Error Invalid Syntax", programLine);
					}
				}
				else if (elem.Equals("break")  || elem.Equals("continue"))
				{
					if (!breakCommand.Equals("") && elem.Equals("continue"))
					{
						parsedProgram.Add((correctionLine, breakCommand));
						startLine++;
					}
					parsedLine = elem;
					parsedLine += " goto ";
					parsedLine += breakLine;
					parsedProgram.Add((programLine, parsedLine));
					startLine++;
					parsedLine = "";
				}
				else
				{
					//This part of the code deals with the non-conditional statements
					//Here is where some early preprocessing is done
					while (i < tokens.Count-1 && !elem.Equals(";"))
					{
						
						if (elem.Equals("++"))
						{
							char nextChar = tokens[i + 1].Item2[0];
							if (Regex.IsMatch(nextChar.ToString(), "[a-z]", RegexOptions.IgnoreCase))
							{
								i++;
								(programLine, elem) = tokens[i];
								parsedProgram.Add((programLine, elem + " = " + elem + " + 1"));
								startLine++;
							}
						}
						if (elem.Equals("--"))
						{
							char nextChar = tokens[i + 1].Item2[0];
							if (Regex.IsMatch(nextChar.ToString(), "[a-z]", RegexOptions.IgnoreCase))
							{
								i++;
								(programLine, elem) = tokens[i];
								parsedProgram.Add((programLine, elem + " = " + elem + " - 1"));
								startLine++;
							}
						}
						if (i != 0)
						{
							do
							{
								parsedLine += elem;
								i++;
								(programLine, elem) = tokens[i];
							} while (elem.Equals(""));
						}
						char start = elem.TrimStart(' ')[0];
						if (Regex.IsMatch(start.ToString(), "[a-z]", RegexOptions.IgnoreCase) && i < tokens.Count - 1 && tokens[i + 1].Item2.Equals("++"))
						{
							incr_decr_lines.Enqueue(elem.TrimStart(' ') + " = " + elem.TrimStart(' ') + " + 1");
							if(!parsedLine.Equals(""))
								parsedLine += elem;
							i = i+2;
							(programLine, elem) = tokens[i];
						}
						else if (Regex.IsMatch(start.ToString(), "[a-z]", RegexOptions.IgnoreCase) && i < tokens.Count - 1 && tokens[i + 1].Item2.Equals("--"))
						{
							incr_decr_lines.Enqueue(elem.TrimStart(' ') + " = " + elem.TrimStart(' ') + " + 1");
							if (!parsedLine.Equals(""))
								parsedLine += elem;
							i = i + 2;
							(programLine, elem) = tokens[i];
						}
						else
						{
							if (i == 0)
							{
								parsedLine += elem;
								i++;
								(programLine, elem) = tokens[i];
							}
						}
						if (!parsedLine.EndsWith(" "))
						{
							if (elem.Equals("="))
							{
								if (parsedLine.EndsWith("+"))
								{
									parsedLine = parsedLine.TrimEnd('+') + elem + " " + parsedLine;
									i++;
									(programLine, elem) = tokens[i];
									continue;
							}
								else if (parsedLine.EndsWith("-"))
								{
									parsedLine = parsedLine.TrimEnd('-') + elem + " " + parsedLine;
									i++;
									(programLine, elem) = tokens[i];
									continue;
							}
								else if (parsedLine.EndsWith("*"))
								{
									parsedLine = parsedLine.TrimEnd('*') + elem + " " + parsedLine;
									i++;
									(programLine, elem) = tokens[i];
									continue;
							}
								else if (parsedLine.EndsWith("/"))
								{
									parsedLine = parsedLine.TrimEnd('/') + elem + " " + parsedLine;
									i++;
									(programLine, elem) = tokens[i];
									continue;
							}
								else elem = " " + elem;
								
						}
						}
						if(parsedLine.EndsWith("="))
						{
							if(!elem.StartsWith(" "))
							{
								parsedLine += " ";
							}
						}
						if (elem.StartsWith("0x"))
						{
							elem = Convert.ToInt32(elem,16).ToString();
						}
						if (elem.StartsWith("0b"))
						{
							elem = Convert.ToInt32(elem.Substring(2), 2).ToString();
						}
						
					}
						if (!parsedLine.Equals("")) { 
							parsedProgram.Add((programLine, parsedLine));
							startLine++;
							parsedLine = "";
						}
						while(incr_decr_lines.Count != 0)
						{
							parsedProgram.Add((programLine, incr_decr_lines.Dequeue()));
							startLine++;
						}
				}
			}
			return parsedProgram;
		}

		private (int,bool) verifyClosedProgram(List<(int,string)> tokens)
		{
			//This method exists to verify that each brace or parenthesis is closed
			//If there is an invalid end this method returns false and the line 
			//number of the invalid end
			


			//Uses a stack to keep track

			Stack<string> bracketStack = new Stack<string>();
			int lastNum = 0;
			foreach((int num, string elem) in tokens)
			{
				lastNum = num;
				if(elem.Equals("{") || elem.Equals("("))
				{
					bracketStack.Push(elem);
				}
				if (elem.Equals("}"))
				{
					if(bracketStack.Count == 0)
					{
						return (num, false);
					}
					if (!bracketStack.Pop().Equals("{"))
					{
						return (num, false);
					}
				}
				if (elem.Equals(")"))
				{
					if (bracketStack.Count == 0)
					{
						return (num, false);
					}
					if (!bracketStack.Pop().Equals("("))
					{
						return (num, false);
					}
				}
			}
			if(bracketStack.Count != 0)
			{
				return (lastNum, false);
			}
			return (-1, true);
		}



		public object EqualsCondition(object a, object b)
		{
			if (a is int)
			{
				if (b is int)
				{
					return (int)a == (int)b;
				}
				else if (b is double)
				{
					return (int)a == (double)b;
				}
				else
				{
					return a != b;
				}
			}
			else if (a is double)
			{
				if (b is int)
				{
					return (double)a == (int)b;
				}
				else if (b is double)
				{
					return (double)a == (double)b;
				}
				else
				{
					return a != b;
				}
			}
			else if (a is string)
			{
				if (b is string)
				{
					return ((string)a).Equals((string)b);
				}
				else
					return a == b;
			}
			else if (a is bool)
			{
				if (b is bool)
				{
					return ((bool)a) == (bool)b;
				}
				else
					return a == b;
			}
			else
				return a == b;
		}
		public object NotEqualsCondition(object a, object b)
		{
			if (a is int)
			{
				if (b is int)
				{
					return (int)a != (int)b;
				}
				else if (b is double)
				{
					return (int)a != (double)b;
				}
				else
				{
					return a != b;
				}
			}
			else if (a is double)
			{
				if (b is int)
				{
					return (double)a != (int)b;
				}
				else if (b is double)
				{
					return (double)a != (double)b;
				}
				else
				{
					return a != b;
				}
			}
			else if(a is string)
			{
				if(b is string)
				{
					return !((string)a).Equals((string)b);
				}
				else
					return a != b;
			}
			else if(a is bool)
			{
				if (b is bool)
				{
					return ((bool)a)!= (bool)b;
				}
				else
					return a != b;
			}
			else
				return a != b;

		}

		public object GreaterThan(object a, object b)
		{
			if (a is int)
			{
				if (b is int)
				{
					return (int)a > (int)b;
				}
				else if (b is double)
				{
					return (int)a > (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Greater Than Comparision",a,b);
				}
			}
			else if (a is double)
			{
				if (b is int)
				{
					return (double)a > (int)b;
				}
				else if (b is double)
				{
					return (double)a > (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Greater Than Comparision",a,b);
				}
			}
			else
			{
				throw new OperationException("Numeric Type Required For Greater Than Comparision",a,b);
			}
		}

		public object GreaterThanOrEquals(object a, object b)
		{
			if (a is int)
			{
				if (b is int)
				{
					return (int)a >= (int)b;
				}
				else if (b is double)
				{
					return (int)a >= (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Greater Than Or Equals Comparision",a,b);
				}
			}
			else if (a is double)
			{
				if (b is int)
				{
					return (double)a >= (int)b;
				}
				else if (b is double)
				{
					return (double)a >= (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Greater Than or Equals Comparision",a,b);
				}
			}
			else
			{
				throw new OperationException("Numeric Type Required For Greater Than or Equals Comparision",a,b);
			}
		}

		public object LessThan(object a, object b)
		{
			if (a is int)
			{
				if (b is int)
				{
					return (int)a < (int)b;
				}
				else if (b is double)
				{
					return (int)a < (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Less Than Comparision",a,b);
				}
			}
			else if (a is double)
			{
				if (b is int)
				{
					return (double)a < (int)b;
				}
				else if (b is double)
				{
					return (double)a < (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Less Than Comparision",a,b);
				}
			}
			else
			{
				throw new OperationException("Numeric Type Required For Less Than Comparision",a,b);
			}
		}

		public object LessThanOrEquals(object a, object b)
		{
			if (a is int)
			{
				if (b is int)
				{
					return (int)a <= (int)b;
				}
				else if (b is double)
				{
					return (int)a <= (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Less Than Or Equals Comparision",a,b);
				}
			}
			else if (a is double)
			{
				if (b is int)
				{
					return (double)a <= (int)b;
				}
				else if (b is double)
				{
					return (double)a <= (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Less Than or Equals Comparision",a,b);
				}
			}
			else
			{
				throw new OperationException("Numeric Type Required For Less Than or Equals Comparision",a,b);
			}
		}
		public object add(object a, object b)
		{
			if(a is int)
			{
				if(b is int)
				{
					return (int)a + (int)b;
				}
				else if(b is double)
				{
					return (int)a + (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Addition",a,b);
				}
			}
			else if (a is double)
			{
				if (b is int)
				{
					return (double)a + (int)b;
				}
				else if (b is double)
				{
					return (double)a + (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Addition",a,b);
				}
			}
			else if(a is string || b is string)
			{
				return a.ToString() + b.ToString();
			}
			else
			{
				throw new Exception("Numeric Type Required For Addition");
			}
		}
		public object subtract(object a, object b)
		{
			if (a is int)
			{
				if (b is int)
				{
					return (int)a - (int)b;
				}
				else if (b is double)
				{
					return (int)a - (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Subtraction",a,b);
				}
			}
			else if (a is double)
			{
				if (b is int)
				{
					return (double)a - (int)b;
				}
				else if (b is double)
				{
					return (double)a - (double)b;
				}
				else
				{
					throw new Exception("Numeric Type Required For Subtraction");
				}
			}
			else
			{
				throw new Exception("Numeric Type Required For Subtraction");
			}
		}
		public object multiply(object a, object b)
		{
			if (a is int)
			{
				if (b is int)
				{
					return (int)a * (int)b;
				}
				else if (b is double)
				{
					return (int)a * (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Multiplication",a,b);
				}
			}
			else if (a is double)
			{
				if (b is int)
				{
					return (double)a * (int)b;
				}
				else if (b is double)
				{
					return (double)a * (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Multiplication",a,b);
				}
			}
			else
			{
				throw new OperationException("Numeric Type Required For Multiplication",a,b);
			}
		}

		public object divide(object a, object b)
		{
			if (a is int)
			{
				if (b is int)
				{
					return (int)a / (int)b;
				}
				else if (b is double)
				{
					return (int)a / (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Division",a,b);
				}
			}
			else if (a is double)
			{
				if (b is int)
				{
					return (double)a / (int)b;
				}
				else if (b is double)
				{
					return (double)a / (double)b;
				}
				else
				{
					throw new OperationException("Numeric Type Required For Division",a,b);
				}
			}
			else
			{
				throw new OperationException("Numeric Type Required For Division",a,b);
			}
		}

		public object power(object a,object b)
		{
			if (a is int)
			{
				if (b is int)
				{
					return Math.Pow((int)a, (int)b);
				}
				else if (b is double)
				{
					return Math.Pow((int)a, (double)b);
				}
				else
				{
					throw new OperationException("Numeric Type Required For Exponential Operation",a,b);
				}
			}
			else if (a is double)
			{
				if (b is int)
				{
					return Math.Pow((double)a, (int)b);
				}
				else if (b is double)
				{
					return Math.Pow((double)a, (double)b);
				}
				else
				{
					throw new OperationException("Numeric Type Required For Exponential Operation",a,b);
				}
			}
			else
			{
				throw new OperationException("Numeric Type Required For Exponential Operation",a,b);
			}
		}

	}
	
}
