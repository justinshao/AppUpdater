namespace Justin.Serialization.Json
{
    using System;
    using System.Security.Permissions;
    using System.Web;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property), AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class ScriptIgnoreAttribute : Attribute
    {
    }
}

