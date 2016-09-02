# scrobble-stats-mechanizer

If you use an audio player or PMP that generates a file in the [Rockbox Audioscrobbler log format](http://www.rockbox.org/wiki/LastFMLog), this program can tag your audio files with listening statistics.

Here's how it works:
* It parses the scrobble log file
* It groups records in that file by Artist + Title
* It determines aggregate listening statistics (First Played, Last Played, Times Finished, Times Skipped, Weighted Rating)
* It saves the statistics back to the audio files as tags

After the files are tagged with these statistics, you can sort/filter your audio collection with a program such as foobar2000 by adding custom columns for FirstPlayed, LastPlayed, TimesFinished, TimesSkipped, and WeightedRating. 

### How to use
Within the ScrobbleStatsMechanizer solution, there is a ScrobblerTagMarker and a ExampleFrontend project. The ScrobbleTagMarker project is the backend that handles all the scrobble file parsing / tag marking logic. The ExampleFrontend project shows a working process for one way you could use the ScrobbleTagMarker. 

You can use the example frontend by building the solution and creating a settings file. See below for details:

1. Build the ScrobbleStatsMechanizer solution in debug mode. 
2. Create a settings.json file in your /ExampleFrontend/bin/Debug folder. See below for an example with comments.
3. Run the program to add listening statistics tags to the audio files.
4. You can re-run at any time to update the files with new statistics.
```
{
	// The volume label to your PMP drive.
	// Used with pmpScrobblerRelativeFilePath to determine the full path to the PMP scrobbler log file.
	"pmpDriveVolumeLabel": "SANSA CLIP",

	// The relative path from the root of the PMP drive to the scrobbler log file.
	// Used with pmpDriveVolumeLabel to determine the full path to the PMP scrobbler log file.
	// In the example-settings.json, this will find drive with label of "SANSA CLIP", then the file in the root named ".scrobbler.log"
	"pmpScrobblerRelativeFilePath": ".scrobbler.log",
	
	// Where to store the master scrobbler file.
	"masterScrobblerFilePath": "D:/ScrobbleFiles/scrobbler.log",

	// Where to save backups of the master scrobbler file.
	"scrobblerBackupsDirectoryPath": "D:/ScrobbleFiles/Backup/",

	// Where your audio collection is stored.
	"audioCollectionDirectoryPath": "D:/Music/"
}
```


### Future plans
* Include another program that can select and copy audio files using customizable rules based on the listening statistics.
