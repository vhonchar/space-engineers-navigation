Space Engineers Navigation (title is TBD)
-----------

Show a map of GPS coordinates on a chosen LCD Panel or Cockpit.

### How To
- build an LCD panel or a cockpit on the same grid as this script
- include `GPS-Map` into name of an LCD Panel or Cockpit
- put list of GPS coordinates into Custom Data of such LCD Panel or Cockpit. Format of GPS marks is the same when you use "Copy to Clipboard" on GPS tab

### Multiple maps
The script supports multiple panels and cockpits, so your grid cana have multiple panels with different list of coordinates to track

### Configuration of a programmable block
Open CustomData of a programmable block for general settings of the script.
The script will prepopulate CustomData with all available options and their default settings.
- `PanelNameContains` - part of name of a LCD Panel or cockpit to render map on it
- `SearchLocalGridOnly` - whether the script should look for compatible panels and cockpits only in this grid, or in attached drids as well (through connectors, hinges, etc)

### Configuration of a map
Open CustomData of a LCD panel or cockpit for map related settings.
The script will prepopulate CustomData with all available options and their default settings.
- `DetectionDistance` - set radius of the map on this screen to the specified value
- `ScreenNumber` - number of a screen in this cockpit to use to show the map. Count starts from 0. **(available only in Cokpit kind of blocks)**

### Adding GPS marks to a map
To add GPS marks to a map, open Custom Data of an LCD panel or cockpit where a map should be shown and put GPS marks in the very end of the data.
Example
```
GPS:Base:12345.67:23456.78:34567.89:
GPS:Mining Site Alpha:11111.22:22222.33:33333.44:This is the primary mining site.
GPS:Ore Deposit Beta:98765.43:87654.32:76543.21:Large iron deposit.
GPS:Wreckage:54321.12:43210.23:32109.34:Abandoned ship wreckage.
```

**Note**, the format of each mark is the same as when you click "Copy to clipboard" in the GPS tab of you interface.
So just copy-paste all marks you would like to track on a map.

**Note 2**, don't forget, this list is individual to each screen. So you can display different list of marks on different screens

### Limitations
- LCD Panel shouold be placed vertically. Back of the panel gonna represent "forward" of a surface, where listed coordinates are progected
