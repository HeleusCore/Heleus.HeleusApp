using System;
using Heleus.Base;

namespace Heleus.Apps.Shared
{
	static class UIAppSettings
	{
        public static bool JoinedProfile = false;
        public static bool SendTransferNotifications = false;

        public static void ReadChunks(ChunkReader reader)
        {
            reader.Read(nameof(JoinedProfile), ref JoinedProfile);
            reader.Read(nameof(SendTransferNotifications), ref SendTransferNotifications);
		}

        public static void WriteChunks(ChunkWriter writer)
        {
            writer.Write(nameof(JoinedProfile), JoinedProfile);
            writer.Write(nameof(SendTransferNotifications), SendTransferNotifications);
		}
    }
}
