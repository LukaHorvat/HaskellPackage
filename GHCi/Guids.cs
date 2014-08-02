// Guids.cs
// MUST match guids.h
using System;

namespace LukaHorvat.GHCi
{
    static class GuidList
    {
        public const string guidGHCiPkgString = "da9352ef-a6d6-4d0f-a2f8-ac96035dc8ad";
        public const string guidGHCiCmdSetString = "8f01d4e3-6b31-4dc4-b695-08b7ac7b3473";
        public const string guidToolWindowPersistanceString = "1c0fae0a-82c1-4658-b3e0-7e68840091c4";

        public static readonly Guid guidGHCiCmdSet = new Guid(guidGHCiCmdSetString);
    };
}