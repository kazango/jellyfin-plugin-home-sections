# Jellyfin Home Sections
Add server provided home sections to your Jellyfin instance.

The public facing name for this plugin (what your users will see) is "Modular Home".

> NOTE: This readme is currently work in progress.

## Getting Started

### Pre-Requisites 

- Jellyfin Version: 10.10.3
- Custom `jellyfin-web` deployed from https://github.com/IAmParadox27/jellyfin-web. There is a release with a packaged version of it for simpler install.
- The `jellyfin-web` directory should be writable by whatever user is running the Jellyfin server instance. If on Windows this might mean changing the permissions for `C:\Program Files\Jellyfin\Server\jellyfin-web` to allow write access the Jellyfin user.
- Plugin Pages (https://github.com/IAmParadox27/jellyfin-plugin-pages) is a required plugin to allow users to define their own settings for what they want to see.

### Installation

1. Add `https://www.iamparadox.dev/jellyfin/plugins/manifest.json` to your plugin repositories
2. Install `Home Screen Sections` from the Catalogue
3. Restart Jellyfin
4. On the user's homepage, open the hamburger menu and you should see a link for settings to "Modular Home". Click this
5. At the top there is a button to enable support and it will retrieve all sections that are available on your instance. Select all that apply
6. Save the settings. _Please note currently the user is not provided any feedback when the settings are saved_
7. Force refresh your webpage (or app) and you should see your new sections instead of the original ones.

### FAQ

#### How can I tell if its worked

> The easiest way to confirm whether the user is using the modular home settings is to check whether the movie posters are portrait or landscape. Due to how the cards are delivered from the backend all cards are forced to be landscape
