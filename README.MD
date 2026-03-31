# PDF Master Index

The PDF Master Index is a tool which scans multiple folders for PDF files and let's you search all of them for single words.

## Quick Start

### Installation with Docker Compose

For manual installation instructions, see [the documentation](doc/usage.md#installation).

- Download the `compose.yaml.template` to the folder you want to have your service in
- Rename it `docker-compose.yml`
- Create a `.env` file in the same folder with the following content:
```
SQL_PASSWORD=YourStrong!Passw0rd
```
- Replace `YourStrong!Passw0rd` with something secure in your file
- _Optional:_ Configure the `database_volume`, so you have configured where SQLServer stores the database 
- Replace the `data` volume with one (or multiple) volumes or mounts that contain your PDF files. \
  It does not matter much where you mount them to, but it's recommended to use the /data folder as data-root.
- _Optional:_ Change the first number of the `ports`-entry to the port you want the service to be visible at
- _Very optional:_ Configure your reverse-proxy to give access to the service

### Add a scan path

- Open the website (by default `http://localhost:8080` or `http://<ip of the machine docker runs on>:8080`) 
- Click on the cogwheel on the top right
- Enter the mounted search path (e.g. `/data/myMount`) \
  This may be a subfolder of your mounted volume
- _Optional:_ Give it a name to display in the UI
- Click the Save-Icon


### Generate the index

- In the widget on the bottom, click the circular arrow to start the scan
- Wait for the scan to complete

### Search

- Enter a word (or part of a word) in the search bar and press enter

## Features

- Multiple scan paths which can be toggled on and off for searching
- Display a list of page numbers for each document a word was found in (if you prefer to open a physical version of the book)
- View found pages directly in the Website
- Navigation between found pages in the PDF viewer
- Free navigation backwards and forwards in the PDF viewer

## Documentation

For detailed usage, see the [documentation](doc/README.MD)

## Contributing

### Writing Code

To contribute code changes, simply fork the repository and create a pull request when you're done.

### Feature requests

For a feature request, create an issue tagged as `feature`.

### Reporting bugs

To report a bug, create an issue tagged as `bug`.

Try to write the exact steps to reproduce the bug, maybe include a screenshot or even a screencast.

I even consider errors in the documentation a bug, so you're welcome to also write an issue when the steps in the documentation don't work.
In this case - especially when talking about installation with docker-compose, please attach a minimal sample docker-compose.yml which causes the bug, so I can reproduce it.
Even then - sometimes it might be easiest to just screencast what you did, so I understand where I wrote the documentation in a misleading manner (and thus made you do the wrong step).

## License

MIT License - see [LICENSE](LICENSE)
