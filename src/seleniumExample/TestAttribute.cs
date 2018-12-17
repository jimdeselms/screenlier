namespace Screenly.SeleniumExample
{
    using System;

    public class TestAttribute : Attribute
    {
        public string Name { get; set; }

        public TestAttribute(string name=null)
        {
            Name = name;
        }
    }
}