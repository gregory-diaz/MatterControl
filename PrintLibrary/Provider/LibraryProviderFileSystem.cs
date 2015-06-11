﻿/*
Copyright (c) 2015, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using MatterHackers.Agg;
using MatterHackers.MatterControl.PrintQueue;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace MatterHackers.MatterControl.PrintLibrary.Provider
{
	public class LibraryProviderFileSystem : LibraryProvider
	{
		private string currentDirectory = ".";
		private List<string> currentDirectoryFiles = new List<string>();
		private string keywordFilter = string.Empty;
		private string rootPath;

		public LibraryProviderFileSystem(string rootPath)
		{
			this.rootPath = rootPath;

			GetFilesInCurrentDirectory();
		}

		public override int CollectionCount
		{
			get { throw new NotImplementedException(); }
		}

		public override int ItemCount
		{
			get
			{
				return currentDirectoryFiles.Count;
			}
		}

		public override string KeywordFilter
		{
			get
			{
				return keywordFilter;
			}

			set
			{
				if (keywordFilter != value)
				{
					keywordFilter = value;
					GetFilesInCurrentDirectory();
					LibraryProvider.OnDataReloaded(null);
				}
			}
		}

		public override void AddCollectionToLibrary(string collectionName)
		{
			throw new NotImplementedException();
		}

		public override void AddFilesToLibrary(IList<string> files, ReportProgressRatio reportProgress = null, RunWorkerCompletedEventHandler callback = null)
		{
			throw new NotImplementedException();
		}

		public override PrintItemWrapper GetPrintItemWrapper(int itemIndex)
		{
			string fileName = currentDirectoryFiles[itemIndex];
			return new PrintItemWrapper(new DataStorage.PrintItem(Path.GetFileNameWithoutExtension(fileName), fileName));
		}

		public override void RemoveCollection(string collectionName)
		{
			throw new NotImplementedException();
		}

		public override void RemoveItem(PrintItemWrapper printItemWrapper)
		{
			throw new NotImplementedException();
		}

		private void GetFilesInCurrentDirectory()
		{
			currentDirectoryFiles.Clear();
			string[] files = Directory.GetFiles(Path.Combine(rootPath, currentDirectory));
			foreach (string filename in files)
			{
				if (ApplicationSettings.LibraryFilterFileExtensions.Contains(Path.GetExtension(filename).ToLower()))
				{
					if (keywordFilter.Trim() == string.Empty
						|| Path.GetFileNameWithoutExtension(filename).Contains(keywordFilter))
					{
						currentDirectoryFiles.Add(filename);
					}
				}
			}
		}
	}
}