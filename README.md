TinyFS.Net
==========

 TinyFS.Net, an embedded file system lib/FS backed by single file on disk.

 Typical usage scenarios:
  * Embedd resources in a single file 
  * Add/Remove content from a single file in run-time
  * Container for files created by your app. e.g. Word creates .doc-files that in turn consists of multiple chunks of data. 
  
 Basic how-to:
  The entry point for TinyFS is the EmbeddedStorage-class. Content is flushed to disk when EmbeddedStorage is disposed. 
  i.e.
	// create instance of EmbeddedStorage
	using (var es = new EmbeddedStorage(@"c:\tmp\somefile.dat")) {
		// Create a file named "myFile"
		var fi = es.CreateFile("myFile");
		// generate some data
		var data = new byte[255];
		for (byte i = 0; i < 255; i++) data[i] = i;
		// write the data to "myFile"
		es.Write(fi, data, 0, 255);
		// read all of "myFile"
		var content = es.Read(fi);
	}

  It's also possible to use the EmbeddedStorageStream for easier data manipulation. EmbeddedStorageStream implements most parts of the abstract Stream class in .Net so it's possible to Seek and Read/Write the same you would when using any other .Net Stream.
	using (var es = new EmbeddedStorage(@"c:\tmp\somefile.dat")) {
		// Create a file named "myFile"
		var ess = new EmbeddedStorageStream("myFile", es);
	}