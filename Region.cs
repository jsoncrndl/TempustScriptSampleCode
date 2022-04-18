using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TempustScript
{
    [DataContract(Name="Region")]
    public class Region : ScriptElement
    {
        [DataMember] private List<ScriptElement> elements;
        [DataMember] private string name;
        [DataMember] private TSScript parent;

        public Region(TSScript parent, string name, List<ScriptElement> elements)
        {
            this.parent = parent;
            this.name = name;
            this.elements = elements;
        }

        public string GetName() { return name; }

        public IEnumerator Execute()
        {
            yield return null;
            foreach (ScriptElement element in elements)
            {
                yield return element.Execute();
            }
            parent.StopExecution(true);
        }
    }
}
