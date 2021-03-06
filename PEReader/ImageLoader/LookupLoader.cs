﻿using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AlphaOmega.Debug
{
	/// <summary>Test loader</summary>
	[Obsolete("This is a test class. Do not use in production enviropment", false)]
	public class LookupLoader : StreamLoader, IDisposable
	{
		Byte[] _map;
		private static String[] _arr = new String[]
		{
			" ", "1", "2", "3", "4", "5", "6", "7", "8", "9", "_10_",
			"_11_", "_12_", "_13_", "_14_", "_15_", "_16_", "_17_", "_18_", "_19_", "_20_",
			"_21_", "_22_", "_23_", "_24_", "_25_", "_26_", "_27_", "_28_", "_29_", "_30_",
		};

		/// <summary>Create instance of test loader</summary>
		/// <param name="filePath"></param>
		public LookupLoader(String filePath)
			: base(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), filePath)
		{
			this._map = new Byte[new FileInfo(filePath).Length];
		}
		
		/// <summary>Get structure from specific padding from the beginning of the image</summary>
		/// <typeparam name="T">Structure type</typeparam>
		/// <param name="padding">Padding from the beginning of the image</param>
		/// <returns>Readed structure from image</returns>
		public override T PtrToStructure<T>(UInt32 padding)
		{
			T result = base.PtrToStructure<T>(padding);

			for(UInt32 loop = padding; loop < padding + (UInt32)Marshal.SizeOf(typeof(T)); loop++)
				_map[loop]++;
			return result;
		}

		/// <summary>Get bytes from specific padding and specific length</summary>
		/// <param name="padding">Padding from the beginning of the image</param>
		/// <param name="length">Length of bytes to read</param>
		/// <returns>Readed bytes</returns>
		public override Byte[] ReadBytes(UInt32 padding, UInt32 length)
		{
			Byte[] result = base.ReadBytes(padding, length);

			for(UInt32 loop = padding; loop < padding + length; loop++)
				_map[loop]++;

			return result;
		}

		/// <summary>Get ACSII string from specific padding from the beginning of the image</summary>
		/// <param name="padding">Padding from the beginning of the image</param>
		/// <returns>String from pointer</returns>
		public override String PtrToStringAnsi(UInt32 padding)
		{
			String result = base.PtrToStringAnsi(padding);

			for(UInt32 loop = padding; loop < padding + result.Length; loop++)
				_map[loop]++;

			return result;
		}

		/// <summary>Save to text file all data that was readed</summary>
		public new void Dispose()
		{
			File.WriteAllText(
				LookupLoader.GetFileUniqueName(this.Source, ".log", 0),
				String.Join(String.Empty, Array.ConvertAll(_map, delegate(Byte b) { return _arr[b]; })));

			base.Dispose();
		}
		/// <summary>Получить уникальное наименование файла</summary>
		/// <param name="path">Путь с наименованием файла</param>
		/// /// <param name="extension">Расширение, которое добавляется к файлу</param>
		/// <param name="index">Индекс наименования, если файл с таким наименованием уже существует</param>
		/// <returns>Уникальное наимеование файла</returns>
		private static String GetFileUniqueName(String path, String extension, UInt32 index)
		{
			String indexName;
			if(index > 0)
				indexName = String.Format("{0}[{1}]{2}", path, index, extension);
			else
				indexName = path + extension;

			if(File.Exists(indexName))
				return LookupLoader.GetFileUniqueName(path, extension, checked(index + 1));
			else
				return indexName;
		}
	}
}