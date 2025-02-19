// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using Prefs;
    using Runtime;
    using Runtime.Utils;

    /// <summary>
    /// Represents locally cached data for the last user that was logged in using the old login system.
    /// <para>
    /// In coherence version 1.3.0 and older, coherence used to store the username, guest password, and session token
    /// for you, and automatically try to log you in again using them.
    /// </para>
    /// <para>
    /// In the current version, you need to store this data yourself, and pass it to the appropriate login methods
    /// in <see cref="AuthClient"/>, to log in again as the same user, without needing to provide the username and password.
    /// Storing the data locally can be done easily using <see cref="Prefs.SetString"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This data can be acquired using <see cref="Get(string,string)"/>, if it exists.
    /// If it doesn't exist, then <see cref="None"/> is returned.
    /// </para>
    /// <para>
    /// You can pass <see cref="SessionToken"/> to <see cref="AuthClient.LoginWithToken"/>
    /// to try to log in again as the same user without needing to provide the username and password.
    /// This will only work if the session token has not expired.
    /// </para>
    /// <para>
    /// You can also pass <see cref="Username"/> and <see cref="GuestPassword"/>
    /// to <see cref="AuthClient.LoginWithPassword"/>, if both are available, to log in again as the same guest user.
    /// If the last user that logged in using the old system used a username and password, then
    /// <see cref="GuestPassword"/> will be an empty string.
    /// </para>
    /// <para>
    /// You can clear all the cached legacy login data for the currenet project using <see cref="Clear(string,string)"/> once you no
    /// longer need it.
    /// </para>
    /// </remarks>
    /// <seealso cref="Get(string,string)"/>
    /// <seealso cref="Clear(string, string)"/>
    /// <seealso cref="AuthClient.LoginWithToken"/>
    /// <seealso cref="AuthClient.LoginWithPassword"/>
    public readonly struct LegacyLoginData
    {
        /// <summary>
        /// Result returned by <see cref="Get(string,string)"/> when no cached data is found for the project.
        /// </summary>
        public static readonly LegacyLoginData None = new("", "", SessionToken.None);

        /// <summary>
        /// Username that was last used to log in using the old login system.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Password that was automatically generated for the last user that was logged in as a guest using the old login system.
        /// </summary>
        public string GuestPassword { get; }

        /// <summary>
        /// Token uniquely identifying the last user that was logged in using the old login system.
        /// </summary>
        public SessionToken SessionToken { get; }

        private LegacyLoginData(string username, string guestPassword, SessionToken sessionToken)
        {
            Username = username;
            GuestPassword = guestPassword;
            SessionToken = sessionToken;
        }

        [Deprecated("15/10/2024", 1, 4, 0, Reason="coherence/unity#6843")]
        internal static void SetCredentials(string projectId, string uniqueId, string username, string guestPassword)
        {
            Prefs.SetString(GetUsernamePrefsKey(projectId, uniqueId), username);
            Prefs.SetString(GetGuestPasswordPrefsKey(projectId, uniqueId), guestPassword);
        }

        /// <summary>
        /// Get locally cached data from the last user that was logged in to the given project using the old login
        /// system in coherence 1.3.0 and older.
        /// </summary>
        /// <returns>
        /// A <see cref="LegacyLoginData"/> object containing the <see cref="LegacyLoginData.Username"/>,
        /// <see cref="LegacyLoginData.GuestPassword"/> (if the last user was logged in as a guest; otherwise, and empty string),
        /// and <see cref="LegacyLoginData.SessionToken"/> for the user, if any; otherwise, <see cref="LegacyLoginData.None"/>.
        /// </returns>
        /// <seealso cref="Clear(string, string)"/>
        public static LegacyLoginData Get(string projectId, string uniqueId = "") =>
            new(
                Prefs.GetString(GetUsernamePrefsKey(projectId, uniqueId), string.Empty),
                Prefs.GetString(GetGuestPasswordPrefsKey(projectId, uniqueId), string.Empty),
                Prefs.GetString(GetSessionTokenPrefsKey(projectId, uniqueId)) is { Length: > 0 } sessionToken
                    ? new(sessionToken)
                    : SessionToken.None
            );

        /// <summary>
        /// Clear all locally cached data for all guest accounts in a given Project that used the the
        /// old login system in coherence 1.3.0 and older.
        /// <para>
        /// Cached data includes the <see cref="LegacyLoginData.Username"/>, <see cref="LegacyLoginData.GuestPassword"/>
        /// (if the last user was logged in as a guest), and <see cref="LegacyLoginData.SessionToken"/> for the user.
        /// </para>
        /// <param name="projectId">ID of the project. Can be retrieved via <see cref="RuntimeSettings.ProjectID">
        /// RuntimeSettings.Instance.ProjectID</see>.</param>
        /// </summary>
        /// <seealso cref="Get(string, string)"/>
        /// <seealso cref="Clear(string,string)"/>
        public static void ClearForProject(string projectId)
        {
            while (UniqueIdPool.TryGet(projectId, out var uniqueId))
            {
                Clear(projectId, uniqueId);
            }
        }

        /// <summary>
        /// Clear all locally cached data from the last user that was logged in to the given project using the old
        /// login system in coherence 1.3.0 and older.
        /// <para>
        /// Cached data includes the <see cref="LegacyLoginData.Username"/>, <see cref="LegacyLoginData.GuestPassword"/>
        /// (if the last user was logged in as a guest), and <see cref="LegacyLoginData.SessionToken"/> for the user-
        /// </para>
        /// </summary>
        /// <param name="projectId"> Identifier of the project whose data cached legacy login data to clear. </param>
        /// <param name="uniqueId"> (Optional) Unique id configured in the main Coherence Bridge. </param>
        /// <seealso cref="Get(string, string)"/>
        /// <seealso cref="RuntimeSettings.ProjectID"/>
        /// <seealso cref="ClearForProject(string)"/>
        public static void Clear(string projectId, string uniqueId)
        {
            Prefs.DeleteKey(GetUsernamePrefsKey(projectId, uniqueId));
            Prefs.DeleteKey(GetGuestPasswordPrefsKey(projectId, uniqueId));
            Prefs.DeleteKey(GetSessionTokenPrefsKey(projectId, uniqueId));
        }

        private static string GetUsernamePrefsKey(string projectId, string uniqueId) => PrefsKeys.CachedLegacyUsername.Format(projectId, uniqueId);
        private static string GetGuestPasswordPrefsKey(string projectId, string uniqueId) => PrefsKeys.CachedLegacyGuestPassword.Format(projectId, uniqueId);
        private static string GetSessionTokenPrefsKey(string projectId, string uniqueId) => PrefsKeys.CachedLegacySessionToken.Format(projectId, uniqueId);
    }
}
