namespace MoreGuns
{
    internal static class Compat
    {
#if IL2CPP
        public static T As<T>(this Il2CppSystem.Object obj) where T : Il2CppSystem.Object
        {
            return obj == null ? null : obj.TryCast<T>();
        }
#else
        public static T As<T>(this object obj) where T : class
        {
            return obj as T;
        }
#endif

        public static string LocalPlayerId()
        {
            try
            {
                return SteamUser.GetSteamID().ToString();
            }
            catch
            {
                return "unknown";
            }
        }

        public static CSteamID LobbySteamId()
        {
            ulong lobbyId = Lobby.Instance != null ? Lobby.Instance.LobbyID : 0UL;
            return new CSteamID(lobbyId);
        }
    }
}
