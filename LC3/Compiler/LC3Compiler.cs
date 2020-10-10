﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LC3.Compiler;

namespace LC3
{
    class LC3Compiler
    {
        private static int lineNum = -1;
        private static readonly List<(int, string)> errors = new List<(int line, string msg)>();

        public static void Main()
        {
            Compile(@"test.txt");
        }

        public static void Compile(string path)
        {
            var instructions = new List<Instruction>();
            int CurrentInstruction = 0;

            string[] parts;


            var InstructionInfo = new Dictionary<string, (int argumentCount, int opcode)>
            {
                ["add"]  = (4, 0x1000),
                ["and"]  = (4, 0x5000),
                ["br"]   = (3, 0x5000),
                ["jmp"]  = (2, 0xC000),
                ["ret"]  = (1, 0xC000),
                ["jsr"]  = (1, 0x4000),
                ["jsrr"] = (1, 0x4000),
                ["ld"]   = (1, 0x2000),
                ["ldi"]  = (1, 0xA000),
                ["ldr"]  = (1, 0x6000),
            };


            foreach (string line in File.ReadLines(path))
            {
                lineNum++;


                parts = line.ToLower().Split(new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                string opcode = parts[0];
                var info = InstructionInfo[opcode];


                // Fix for invalid BR syntax used in class, BRnzp 0 -> BR nzp 0
                if (opcode.Substring(0, 2) == "br" && parts.Length == 2)
                {
                    parts = new string[] { "br", opcode.Substring(2), parts[1] };
                    opcode = "br";
                }

                if (BadSyntaxCheck(parts.Length != info.argumentCount
                    , $"Invalid {opcode.ToUpper()} instruciton, needs {info.argumentCount} arguments."))
                    continue;

                


                switch (opcode) 
                {   
                    case "add": // ADD r0 r0 r0
                    case "and": // ADD r0 r0 0
                        CurrentInstruction = parts[3][0] == 'r'
                            ? info.opcode + Register(1, 9) + Register(2, 6) + Register(3, 0)                           
                            : info.opcode + Register(1, 9) + Register(2, 6) + 0x0020 + Offset(3, 5);                   
                        break;

                    case "br": // BR nzp 0
                        int conditions = (parts[1].Contains("n") ? 0x0800 : 0) +
                                         (parts[1].Contains("z") ? 0x0400 : 0) +
                                         (parts[1].Contains("p") ? 0x0200 : 0);
                        CurrentInstruction = info.opcode + conditions + Offset(2, 9);                                  
                        break;

                    case "jmp": // JMP r0
                        CurrentInstruction = info.opcode + Register(1, 6);                                             
                        break;
                    case "ret": // RET
                        CurrentInstruction = info.opcode + 0x01C0;                                                     
                        break;

                    case "JSR": // JSR 0
                        CurrentInstruction = info.opcode + 0x0800 + Offset(1, 11);
                        break;
                    case "JSRR": // JSRR r0
                        CurrentInstruction = info.opcode + Register(1, 6);
                        break;

                    case "LD": // JSR 0
                        CurrentInstruction = info.opcode + 0x0800 + Offset(1, 11);
                        break;
                    case "LDI": // JSRR r0
                        CurrentInstruction = info.opcode + Register(1, 6);
                        break;




                    default:
                        BadSyntaxCheck(true, $"Instruction '{opcode}' not recognized.");
                        break;
                }

                
                instructions.Add(new Instruction(CurrentInstruction));
            }


            int Register(int partNum, int offset = 0)
            {
                int reg = parts[partNum][1] - '0';

                if (BadSyntaxCheck(reg < 0 && reg > 7, "Invalid register number: " + parts[partNum]))
                    return 0;

                return reg << offset;
            }

            int Offset(int partNum, int length)
            {
                BadSyntaxCheck(!int.TryParse(parts[partNum], out int offset), "Cannot parse offset: " + parts[partNum]);
                if (BadSyntaxCheck(offset >= (1 << length), $"Offset '{offset}' to large to fit in {length} bits."))
                    return 0;

                return offset;
            }
        }


        


        /// <summary>
        ///     Adds error and returns True if check is True.
        /// </summary>
        /// <param name="check">True for bad syntax. </param>
        /// <param name="errorMsg">Error message to be added if assertion is False. </param>
        /// <returns>True if error was added</returns>
        private static bool BadSyntaxCheck(bool check, string errorMsg)
        {
            if (check)
                errors.Add((lineNum, errorMsg));

            return check;
        }
    }
}