using System;

namespace AutoNumber
{
    internal static class ETag
    {
        public static Azure.ETag ForDate(DateTime input)
        {
            return new Azure.ETag($"W/\"{input.Ticks}\"");
        }
    }
}