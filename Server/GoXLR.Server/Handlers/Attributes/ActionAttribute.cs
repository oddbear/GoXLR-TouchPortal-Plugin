using System;

namespace GoXLR.Server.Handlers.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ActionAttribute : Attribute
    {
        public string Action { get; }

        public ActionAttribute(string actionName)
        {
            Action = actionName;
        }
    }
}
