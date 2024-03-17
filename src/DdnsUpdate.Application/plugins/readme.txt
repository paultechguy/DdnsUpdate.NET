This directory contains sub-directories of DdnsUpdate.NET plugins.
Each plugin implements the interface from IDdnsUpdateProvider.

When DdnsUpdate.NET starts up, it will dynamically loads all
plugins in all subdirectories.  To disable a plugin, delete,
move, or "comment out" a plugin.  You can comment out a plugin
by starting the directory name with a '#' symbol.
