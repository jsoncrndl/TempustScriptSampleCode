using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TempustScriptInterpreter
{
    public class Region : ScriptElement
    {
        [JsonInclude] public List<ScriptElement> elements;
        private string name { get; }
        public string Name { get { return name; } }

        public Region(PCScript parent, string name, List<ScriptElement> elements)
        {
            this.name = name;
            this.elements = elements;
        }

        public bool Execute()
        {
            foreach (ScriptElement element in elements)
            {
                element.Execute();
            }
            return true;
        }
    }
}
