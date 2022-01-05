using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using TempustScriptInterpreter.InterpreterException;

namespace TempustScriptInterpreter
{
    public class Interpreter
    {
        static void Main(string[] args)
        {
            string filePath = Path.Combine(Environment.CurrentDirectory, @"data\test.tmpst");

            List<string> askTest = new List<string> {
            "ask[\"Village Girl\"] \"Have you seen my dog?\"",
            "{",
            "\topt \"No\"",
            "\t{",
            "\t\tsay \"That's too bad\"",
            "\t}",
            "\topt \"Yes\"",
            "\t{",
            "\t\tsay \"Thank you!\"",
            "\t}",
            "}"
            };

            //Block.AskBlock block = BlockFactory.MakeAskBlock(null, askTest);
            //TestConditions();

            DataSerializer.Serialize(MakeScript(filePath), Path.GetFileNameWithoutExtension(filePath), new DESCryptoServiceProvider());
            PCScript script = DataSerializer.Deserialize(Path.Combine(Environment.CurrentDirectory, @"test.pc"), new DESCryptoServiceProvider());
        }

        public static void TestConditions()
        {
            PCScript pc = new PCScript();
            pc.SetLocalFlag("flag1", false);
            pc.SetLocalFlag("flag2", true);
            pc.SetLocalFlag("flag3", true);
            pc.SetLocalFlag("flag4", false);
            Block.ConditionalBlock block = new Block.ConditionalBlock(pc, "check local flag1 or check local flag2 and check local flag3 or check local flag4", null);
            //bool met = block.ConditionMet();
        }

        public static PCScript MakeScript(string filePath)
        {
            PCScript script = new PCScript();
            string[] lines = File.ReadAllLines(filePath);
            SeparateRegions(script, lines);
            return script;
        }

        /**
         * Separate and add regions to the parent. This creates all blocks and commands in the script.
         */
        private static void SeparateRegions(PCScript parent, string[] lines)
        {
            int curLine = 0;
            List<string> defaultRegion = new List<string>();
            while (curLine < lines.Length)
            {
                if (lines[curLine].Trim().Split(" ")[0].Equals("region"))
                {
                    int startLine = curLine;
                    List<string> regionLines = new List<string>();
                    while (!lines[curLine].Trim().Equals("endregion"))
                    {
                        if (!lines[curLine].Trim().Equals(""))
                        {
                            regionLines.Add(lines[curLine]);
                        }
                        curLine++;

                        if (curLine >= lines.Length)
                        {
                            throw new InvalidRegionException(String.Format("Region starting on line {0} has no end", startLine + 1));
                        }
                    }

                    //Add the endregion command
                    regionLines.Add(lines[curLine]);
                    parent.AddRegion(RegionFactory.MakeRegion(parent, regionLines));
                }
                else if (!lines[curLine].Trim().Equals(""))
                {
                    defaultRegion.Add(lines[curLine]);
                }
                curLine++;
            }
            
            parent.AddRegion(new Region(parent, "default", ReadLines(parent, defaultRegion)));
        }

        /**
         * Separate lines of tempust script into blocks and commands with the given parent.
         */
        public static List<ScriptElement> ReadLines(PCScript parent, List<string> lines)
        {
            List<ScriptElement> elements = new List<ScriptElement>();
            //Check for blocks. Blocks start with ask[], say[], check, checknot, or op1, and will always have { as the next line, and will end with }.
            int currentLine = 0;
            while (currentLine < lines.Count)
            {
                //If this line or the next line isn't {, treat it as a command. Errors will be found later
                if (lines[currentLine].Trim() != "{" && (currentLine + 1 == lines.Count || (currentLine + 1 < lines.Count && !lines[currentLine + 1].Trim().Equals("{"))))
                {
                    elements.Add(CommandFactory.MakeCommand(parent, lines[currentLine]));
                }
                else if (lines[currentLine].Trim().Equals("{"))
                {
                    List<string> blockLines = new List<string>();
                    blockLines.Add(lines[currentLine - 1]); //Add the previous line. This is the header/command
                    blockLines.Add(lines[currentLine]);
                    currentLine++;
                    int startLine = currentLine - 1;
                    int depth = 0;


                    //Start building the block. Depth with an else block can be a bit confusing. The } before the else moves depth to -1, then the { after else moves it back to 0.
                    while (!lines[currentLine].Trim().Equals("}") || depth != 0 || (currentLine + 1 < lines.Count && lines[currentLine + 1].Trim().Equals("else")))
                    {
                        if (lines[currentLine].Trim().Equals("{"))
                        {
                            depth++;
                        }
                        else if (lines[currentLine].Trim().Equals("}"))
                        {
                            depth--;
                        }

                        blockLines.Add(lines[currentLine]);
                        currentLine++;

                        if (currentLine >= lines.Count)
                        {
                            throw new InvalidBlockException(String.Format("Block starting at line {0} has no end", startLine));
                        }
                    }

                    blockLines.Add(lines[currentLine]);
                    elements.Add(BlockFactory.MakeBlock(parent, blockLines));
                }
                currentLine++;
            }

            return elements;
        }
    }
}
