# Usage of PDF Master Index

## Installation 

### Manual

- You will need to have an SQLServer running or install the SQLServer Express LocalDB driver
- Download the application .ZIP archive (or build yourself)
- Update the connection string in `appSettings.json` to match your SQLServer (or use the one from `appSettings.Development.json` if you installed the LocalDB driver)
- Delete `appSettings.Development.json` 
- Run the application

### Docker Compose (preferred)

- Download the `compose.yaml.template` to the folder you want to have your service in
- Rename it `docker-compose.yml`
Create a `.env` file in the same folder with the following content:
```
SQL_PASSWORD=YourStrong!Passw0rd
```
- Replace `YourStrong!Passw0rd` with something secure in your file
- _Optional:_ Configure the `database_volume`, so you have configured where SQLServer stores the database 
- Replace the `data` volume with one (or multiple) volumes or mounts that contain your PDF files. \
  It does not matter much where you mount them to, but it's recommended to use the /data folder as data-root.
- _Optional:_ Change the first number of the `ports`-entry to the port you want the service to be visible at
- _Very optional:_ Configure your reverse-proxy to give access to the service

## Updating

### Manually

- Download the latest version of the application (or build yourself)
- Replace the files in the folder you installed the application in \
  **WARNING:** Make sure that you preserve the custom connection string in `appSettings.json`
- Run the application

### Docker Compose (Preferred)

- Run `docker compose pull` \
  This will download the latest versions of the containers
- Run `docker compose down` \
  This will stop and delete the containers, preserving the volumes
- Run `docker compose up -d` \
  This will recreate the containers and start the service again

## Managing scan paths

In order to manage scan paths you need to switch to the settings page which is accessible through the cogwheel icon in the top-left corner.
The cogwheel will turn into an "X" which brings you back to the main page.

### Adding a scan path

Before you can add a scan path you need to ensure that the service can _see_ it. This means that if it runs in a docker-container, the path needs to be mounted into the container.
Also it means that when you enter the path, you need to enter the location it is mounted to inside the container. 

If you run the master index directly on your machine, you can just use any physical (or UNC, at least on Windows) path directly.

- Enter the path you want to (recursively) scan for PDFs into the first column of the last row of the table.
- Optionally enter a display name which will be shown by the filter
- Click the Save-Icon

As soon as a new "empty" row appears, your scan path is added

### Removing a scan path

- Click the little trash can next to the entry you want to delete
- You will need to confirm the action
- Once the row has vanished, the entry and all its documents are removed from the index \
  Of course the PDF files will still stay untouched

### Changing a scan path

To change a scan path just make the changes you want to make (i.e. changing the display name or the path itself) and click on the disk icon.

If you made a mistake, you can revert to the data in the database by clicking the revert icon between the disk and the trash-can.

## Indexing documents

To index the documents from the scan paths you have configured, click the little refresh-icon in the top right corner of the widget at the bottom of the screen.

Indexing can take a while and it happens in two stages:

1) All configured scan paths are scanned for PDF documents.

   In this stage the master index checks if documents have been added, changed or removed. Added or changed documents are queued for the next stage and removed documents are removed from the database.

   Here only the upper progress bar has a relevant meaning: How many of the scan paths have been processed
3) All queued documents are indexed

   This is the actual slow operation. It reads each word in the document and adds its location into the database, so you can search for the word.
   
   Here the puper progress bar shows how many of the documents have been processed and the lower progress bar shows the progress of the current document

You can close the website while indexing runs as it is independent of the UI.

You can stop indexing by clicking on the `X` icon in the top right corner of the indexing widget.

Once indexing is completed (or has been aborted), the widget will hide the progress and go back to its idle-state.

## Searching for word

### Simple search and how to interpret search results

Just enter text into the search bar and either press enter, return, click the magnifying glass or wait half a second to perform a search.

The master index will now display all words that contain the text entered, showing an exact match at the top, if the entered text was found as word in your PDFs.

Click the word to reveal or hide in which documents it appears and on which pages it was found in that document. 
If you click the icon, the document viewer will be opened on the first page that contains the word.
For more on the document viewer, see the chapter `The document viewer`

### Search for multiple words

The index does not support searching for more than one word

### Search only in a subset of documents

You can limit search to a set of scan paths. 

By clicking on the tools icon to the left of the magnifying glass, a popup opens which shows all scan paths. 
This is why you can configure a name for each scan path as this list shows the names of the scan paths.

By clicking an entry you can toggle it on and off. 
Search is only performed in scan paths which are turned on.

## The document viewer

By clicking on the icon next to a document in the search results, the document viewer will open.
You can close it by clicking the `X` in its top right corner.

It will start out displaying the first page where the word which the search result belongs to was found.
Occurrences of the searched word will (roughly) be highlighted in yellow, so you can move your attention to the right places on the page.

### Zoom and Pan

You can use the mouse wheel to zoom and click & drag to pan the document.

### Navigation

There are two ways to navigate within a document. 

Navigation always resets zoom and pan.

#### Navigation between search results

Above the displayed page, you find the list of pages associated with the search result you clicked.
To navigate to one of these pages, just click on the button with the page number.

If that list is too long to display in one line, you will see small arrows to its left and/or right.
This means that you can either click them to see more entries or just click and drag them to scroll through the list.

#### Navigation between single pages

Alternatively you can move just a single page within the document with the big left and right arrows in the viewing area.
This allows you to see the page(s) before and after a search result, in case the text relevant for you appears there.




