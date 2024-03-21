This directory contains sub-directories of DdnsUpdate.NET plugins.
Each plugin implements the IDdnsUpdatePlugin interface.

When DdnsUpdate.NET starts up, it will dynamically loads all
plugins in all subdirectories.  To disable a plugin, delete it,
move it, or "comment out" a plugin.  To comment out a plugin,
the directory name should begin with a '#' symbol.
