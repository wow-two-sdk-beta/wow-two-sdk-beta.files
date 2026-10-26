# WoW2.Sdk.Beta.Be.Files.Server

The Files server role over the Files module.

- delivery: private files through signed `/f/{id}` URLs, public files at immutable `/m/{sha256}` URLs, range requests
- shares and file requests: `/s/{token}` and `/r/{token}` pages behind their gates
- upload protocols beside the API's multipart upload
- tenancy and the admin API of a shared files host

A product that runs Form 1 maps the routes in its own host; the `files` service image maps them for Forms 2 and 3.

```bash
dotnet add package WoW2.Sdk.Beta.Be.Files.Server --prerelease
```

Source: https://github.com/wow-two-sdk-beta/wow-two-sdk-beta.files
