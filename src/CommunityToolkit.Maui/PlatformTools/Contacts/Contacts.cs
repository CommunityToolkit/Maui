using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Essentials;
using static Microsoft.Maui.Essentials.Permissions;

namespace CommunityToolkit.Maui.PlatformTools
{
    public static partial class Contacts
    {
        public static async Task<Contact> PickContactAsync()
        {
            // iOS does not require permissions for the picker
            if (DeviceInfo.Platform != DevicePlatform.iOS)
                await EnsureContactsReadGranted();

            return await PlatformPickContactAsync();
        }

        public static Task<IEnumerable<Contact>> GetAllAsync(CancellationToken cancellationToken = default)
            => PlatformGetAllAsync(cancellationToken);

        static async Task EnsureContactsReadGranted()
        {
            var status = await RequestAsync<ContactsRead>()

            if (status is not PermissionStatus.Granted)
                throw new PermissionException($"{typeof(Permissions.ContactsRead).Name} permission was not granted: {status}");
        }
    }
}
