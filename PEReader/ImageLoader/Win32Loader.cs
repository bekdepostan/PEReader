﻿using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace AlphaOmega.Debug
{
	/// <summary>HMODULE loader class</summary>
	[DefaultProperty("Source")]
	public class Win32Loader : IImageLoader
	{
		internal class HModuleHandle : SafeHandleZeroOrMinusOneIsInvalid
		{
			private HModuleHandle()
				: base(true)
			{
			}
			[ReliabilityContract(Consistency.WillNotCorruptState,Cer.MayFail)]
			protected override Boolean ReleaseHandle()
			{
				return NativeMethods.FreeLibrary(base.handle);
			}
		}
		private Boolean _freeOnClose = false;
		private IntPtr? _hModule;
		/// <summary>Module mapped to memory</summary>
		public Boolean IsModuleMapped { get { return true; } }
		/// <summary>Базовый адрес загруженного модуля</summary>
		/// <exception cref="ObjectDisposedException">Object disposed</exception>
		public Int64 BaseAddress
		{
			get
			{
				if(this._hModule == null)
					throw new ObjectDisposedException("_hModule");

				return this._hModule.Value.ToInt64();
			}
		}
		/// <summary>Источник получения PE модуля</summary>
		public String Source
		{
			get;
			private set;
		}
		/*public Win32Loader(String filePath)
		{
			if(String.IsNullOrEmpty(filePath))
				throw new ArgumentNullException("filePath");
			else if(!File.Exists(filePath))
				throw new FileNotFoundException(String.Format("File {0} not found", filePath));

			this.Source = filePath;
			//TODO: DLL: C:\WINDOWS\System32\Mpegc32f.dll не читаейтся через LoadLibraryEx
			this._hModule = NativeMethods.LoadLibraryEx(this.Source, IntPtr.Zero, NativeMethods.LoadLibraryFlags.DONT_RESOLVE_DLL_REFERENCES);
			//this._hModule = NativeMethods.GetModuleHandle(this.Source);
			if(this._hModule == IntPtr.Zero)
				throw new Win32Exception();

			this._freeOnClose = true;
			this.IsImageLoaded = this._hModule != IntPtr.Zero;
		}*/
		/// <summary>Load image from file</summary>
		/// <param name="filePath">Path to file</param>
		/// <exception cref="T:ArgumentNullException">filePath is null</exception>
		/// <exception cref="T:FileNotFoundException">File on path filePath not found</exception>
		/// <exception cref="T:Win32Exception">LoadLibrary can't load PE/PE+ image</exception>
		/// <returns>Loader</returns>
		public static Win32Loader FromFile(String filePath)
		{
			if(String.IsNullOrEmpty(filePath))
				throw new ArgumentNullException("filePath");
			else if(!File.Exists(filePath))
				throw new FileNotFoundException(String.Format("File {0} not found", filePath));

			IntPtr hModule = NativeMethods.LoadLibraryEx(filePath, IntPtr.Zero, NativeMethods.LoadLibraryFlags.DONT_RESOLVE_DLL_REFERENCES);
			if(hModule == IntPtr.Zero)
				throw new Win32Exception();

			return new Win32Loader(hModule, filePath, false);
		}
		/// <summary>Load image from HModule</summary>
		/// <param name="module">Loaded module</param>
		/// <exception cref="T:ArgumentNullException">module is null</exception>
		/// <returns>Loader</returns>
		public static Win32Loader FromModule(ProcessModule module)
		{
			if(module == null)
				throw new ArgumentNullException("module");

			return new Win32Loader(module.BaseAddress,
				module.FileName,
				false);
		}
		/// <summary>Create instance of HMODULE loader class</summary>
		/// <param name="hModule">HMODULE</param>
		public Win32Loader(IntPtr hModule)
			: this(hModule, "Memory", false)
		{
		}
		/// <summary>Create instance of HMODULE loader class</summary>
		/// <param name="hModule">HMODULE</param>
		/// <param name="source">Source of file</param>
		/// <param name="freeOnClode">Close HMODULE after exit</param>
		/// <exception cref="T:ArgumentNullException">hModule is empty</exception>
		/// <exception cref="T:ArgumentNullException">source is null</exception>
		public Win32Loader(IntPtr hModule, String source, Boolean freeOnClode)
		{
			if(hModule == IntPtr.Zero)
				throw new ArgumentNullException("hModule");
			if(String.IsNullOrEmpty(source))
				throw new ArgumentNullException("source");

			this.Source = source;
			this._hModule = hModule;
			this._freeOnClose = freeOnClode;
		}
		/// <summary>Получить массив байт с начала отступа</summary>
		/// <param name="padding">Отступ от начала файла или RVA</param>
		/// <param name="length">Читаемый размер</param>
		/// <returns>Получить массив байт с отступа</returns>
		public Byte[] ReadBytes(UInt32 padding, UInt32 length)
		{
			IntPtr target = new IntPtr(this.BaseAddress + padding);
			Byte[] result = new Byte[length];

			Marshal.Copy(target, result, 0, (Int32)length);
			return result;
		}
		/// <summary>Получить структуру с определённого отступа</summary>
		/// <typeparam name="T">Структура</typeparam>
		/// <param name="padding">Отступ от начала файла или RVA</param>
		/// <returns>Прочитанная структура</returns>
		public T PtrToStructure<T>(UInt32 padding) where T : struct
		{
			IntPtr target = new IntPtr(this.BaseAddress + padding);
			return (T)Marshal.PtrToStructure(target, typeof(T));
		}
		/// <summary>Получить строку с отпределённого отступа</summary>
		/// <param name="padding">Отступ от начала файла или RVA</param>
		/// <returns>Прочитанная строка</returns>
		public String PtrToStringAnsi(UInt32 padding)
		{
			return Marshal.PtrToStringAnsi(new IntPtr(this.BaseAddress + padding));
		}
		/// <summary>Free HMODULE</summary>
		/// <exception cref="T:Win32Exception">Can't unload library</exception>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		/// <summary>Dispose managed objects</summary>
		/// <param name="disposing">Dispose managed objects</param>
		protected virtual void Dispose(Boolean disposing)
		{
			if(disposing && this._hModule.HasValue)
			{
				if(this._freeOnClose && !NativeMethods.FreeLibrary(this._hModule.Value))
					throw new Win32Exception();
				this._hModule = null;
			}
		}
	}
}