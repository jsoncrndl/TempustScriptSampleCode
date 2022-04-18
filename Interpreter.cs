using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TempustScript.InterpreterException;

namespace TempustScript
{
    public class Interpreter
    {
        public static TSScript MakeScript(string filePath)
        {
            TSScript script = new TSScript();
            string content = File.ReadAllText(filePath);

            content = Regex.Replace(content, "(//.*$)|(/\\*[\\s\\S]*?\\*/)", "", RegexOptions.Multiline);

            string[] lines = content.Split('\n');

            SeparateRegions(script, lines);
            return script;
        }

        /**
         * Separate and add regions to the parent. This creates all blocks and commands in the script.
         */
        private static void SeparateRegions(TSScript parent, string[] lines)
        {
            int curLine = 0;
            List<string> defaultRegion = new List<string>();
            while (curLine < lines.Length)
            {
                if (lines[curLine].Trim().Split(' ')[0].Equals("region"))
                {
                    int startLine = curLine;
                    List<string> regionLines = new List<string>();
                    while (!lines[curLine].Trim().Equals("endregion"))
                    {
                        if (!lines[curLine].Trim().Equals(""))
                        {
                            regionLines.Add(lines[curLine].Trim('\r', '\n'));
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
                    defaultRegion.Add(lines[curLine].Trim('\r', '\n'));
                }
                curLine++;
            }
            
            parent.AddRegion(new Region(parent, "default", ReadLines(parent, defaultRegion)));
        }

        /**
         * Separate lines of tempust script into blocks and commands with the given parent.
         */
        public static List<ScriptElement> ReadLines(TSScript parent, List<string> lines)
        {
            List<ScriptElement> elements = new List<ScriptElement>();
            //Check for blocks. Blocks start with ask[], say[], check, checknot, or op1, and will always have { as the next line, and will end with }.
            int currentLine = 0;
            while (currentLine < lines.Count)
            {
                //If this line or the next line isn't {, treat it as a command. Errors will be found later
                if (lines[currentLine].Trim() != "{" && (currentLine + 1 == lines.Count || (currentLine + 1 < lines.Count && !lines[currentLine + 1].Trim().Equals("{"))))
                {
                    if (lines[currentLine].StartsWith("def"))
                    {
                        parent.AddObject(lines[currentLine].Split(" ".ToCharArray(), 2)[1]);
                    }
                    else
                    {
                        elements.Add(CommandFactory.MakeCommand(parent, lines[currentLine]));
                    }
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
