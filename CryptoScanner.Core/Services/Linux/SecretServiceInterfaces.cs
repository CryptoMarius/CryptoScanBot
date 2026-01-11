using Tmds.DBus;

namespace CryptoScanner.Core.Services.Linux;

// org.freedesktop.secrets
[DBusInterface("org.freedesktop.secrets")]
interface ISecretService : IDBusObject
{
    Task<(ObjectPath, object)> OpenSessionAsync(string algorithm, object input);

    Task<ObjectPath> CreateItemAsync(
        ObjectPath collection,
        IDictionary<string, object> properties,
        Secret secret,
        bool replace);
}

// org.freedesktop.Secret.Collection
[DBusInterface("org.freedesktop.Secret.Collection")]
interface ISecretCollection : IDBusObject
{
    Task<(ObjectPath[] Unlocked, ObjectPath[] Locked)> SearchItemsAsync(
        IDictionary<string, string> attributes);
}

// org.freedesktop.Secret.Item
[DBusInterface("org.freedesktop.Secret.Item")]
interface ISecretItem : IDBusObject
{
    Task<Secret> GetSecretAsync(ObjectPath session);
}
