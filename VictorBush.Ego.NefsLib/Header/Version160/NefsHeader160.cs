// See LICENSE.txt for license information.

using System.Text;
using VictorBush.Ego.NefsLib.Header.Version150;
using VictorBush.Ego.NefsLib.IO;
using VictorBush.Ego.NefsLib.Utility;

namespace VictorBush.Ego.NefsLib.Header.Version160;

/// <inheritdoc cref="INefsHeader" />
public sealed class NefsHeader160 : INefsHeader, IFormattable
{
	public NefsWriterSettings WriterSettings { get; }
	public NefsTocHeaderA160 Intro { get; }
	public NefsTocHeaderB160 TableOfContents { get; }
	public NefsHeaderEntryTable160 EntryTable { get; }
	public NefsHeaderSharedEntryInfoTable160 SharedEntryInfoTable { get; }
	public NefsHeaderNameTable NameTable { get; }
	public NefsHeaderBlockTable151 BlockTable { get; }
	public NefsHeaderVolumeInfoTable150 VolumeInfoTable { get; }
	public NefsHeaderWriteableEntryTable160 WriteableEntryTable { get; }
	public NefsHeaderWriteableSharedEntryInfoTable160 WriteableSharedEntryInfoTable { get; }
	public NefsHeaderHashDigestTable160 HashDigestTable { get; }

	/// <inheritdoc />
	public NefsVersion Version => (NefsVersion)Intro.Version;

	/// <inheritdoc />
	public bool IsLittleEndian => WriterSettings.IsLittleEndian;

	/// <inheritdoc />
	public bool IsEncrypted => WriterSettings.IsEncrypted;

	/// <inheritdoc />
	public byte[] AesKey => Intro.AesKey.GetAesKey();

	/// <inheritdoc />
	public Sha256Hash Hash => Intro.Hash;

	/// <inheritdoc />
	public uint Size => Intro.TocSize;

	/// <inheritdoc />
	public uint BlockSize => TableOfContents.BlockSize;

	/// <inheritdoc />
	public uint SplitSize => TableOfContents.SplitSize;

	/// <inheritdoc />
	public uint NumEntries => Intro.NumEntries;

	/// <inheritdoc />
	public IReadOnlyList<VolumeInfo> Volumes { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="NefsHeader160"/> class.
	/// </summary>
	public NefsHeader160(
		NefsWriterSettings writerSettings,
		NefsTocHeaderA160 intro,
		NefsTocHeaderB160 toc,
		NefsHeaderEntryTable160 entryTable,
		NefsHeaderSharedEntryInfoTable160 sharedEntryInfoTable,
		NefsHeaderNameTable nameTable,
		NefsHeaderBlockTable151 blockTable,
		NefsHeaderVolumeInfoTable150 volumeInfoTable,
		NefsHeaderWriteableEntryTable160 writeableEntryTable,
		NefsHeaderWriteableSharedEntryInfoTable160 writeableSharedEntryInfoTable,
		NefsHeaderHashDigestTable160 hashDigestTable)
	{
		WriterSettings = writerSettings;
		Intro = intro;
		TableOfContents = toc;
		EntryTable = entryTable ?? throw new ArgumentNullException(nameof(entryTable));
		SharedEntryInfoTable = sharedEntryInfoTable ?? throw new ArgumentNullException(nameof(sharedEntryInfoTable));
		NameTable = nameTable ?? throw new ArgumentNullException(nameof(nameTable));
		BlockTable = blockTable ?? throw new ArgumentNullException(nameof(blockTable));
		VolumeInfoTable = volumeInfoTable ?? throw new ArgumentNullException(nameof(volumeInfoTable));
		WriteableEntryTable = writeableEntryTable ?? throw new ArgumentNullException(nameof(writeableEntryTable));
		WriteableSharedEntryInfoTable = writeableSharedEntryInfoTable ?? throw new ArgumentNullException(nameof(writeableSharedEntryInfoTable));
		HashDigestTable = hashDigestTable ?? throw new ArgumentNullException(nameof(hashDigestTable));

		Volumes = VolumeInfoTable.Entries.Select(x => new VolumeInfo
		{
			Size = x.Size,
			Name = NameTable.FileNamesByOffset[x.NameOffset],
			DataOffset = x.DataOffset
		}).ToArray();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NefsHeader160"/> class.
	/// </summary>
	internal NefsHeader160() : this(new NefsWriterSettings(), new NefsTocHeaderA160(), new NefsTocHeaderB160(),
		new NefsHeaderEntryTable160([]), new NefsHeaderSharedEntryInfoTable160([]), new NefsHeaderNameTable(),
		new NefsHeaderBlockTable151([]), new NefsHeaderVolumeInfoTable150([]), new NefsHeaderWriteableEntryTable160([]),
		new NefsHeaderWriteableSharedEntryInfoTable160([]), new NefsHeaderHashDigestTable160([]))
	{
	}

	/// <inheritdoc/>
	public string GetFileName(uint nameOffset)
	{
		return NameTable.FileNamesByOffset[nameOffset];
	}

	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		if (format is null)
		{
			return ToString() ?? string.Empty;
		}

		if (format is not "DBG")
		{
			throw new ArgumentException("Format is not supported.", nameof(format));
		}

		var headerPart1String = new StringBuilder();
		foreach (var entry in EntryTable.Entries)
		{
			headerPart1String.Append($"0x{entry.Start.ToString("X", formatProvider)}".PadRight(20));
			headerPart1String.Append($"0x{entry.SharedInfo.ToString("X", formatProvider)}".PadRight(20));
			headerPart1String.Append($"0x{entry.FirstBlock.ToString("X", formatProvider)}".PadRight(20));
			headerPart1String.Append($"0x{entry.NextDuplicate.ToString("X", formatProvider)}".PadRight(20));
			headerPart1String.AppendLine();
		}

		var headerPart2String = new StringBuilder();
		foreach (var entry in SharedEntryInfoTable.Entries)
		{
			headerPart2String.Append($"0x{entry.Parent.ToString("X", formatProvider)}".PadRight(20));
			headerPart2String.Append($"0x{entry.FirstChild.ToString("X", formatProvider)}".PadRight(20));
			headerPart2String.Append($"0x{entry.NameOffset.ToString("X", formatProvider)}".PadRight(20));
			headerPart2String.Append($"0x{entry.Size.ToString("X", formatProvider)}".PadRight(20));
			headerPart2String.Append($"0x{entry.FirstDuplicate.ToString("X", formatProvider)}".PadRight(20));
			headerPart2String.AppendLine();
		}

		var headerPart3String = new StringBuilder();
		foreach (var s in NameTable.FileNames)
		{
			headerPart3String.AppendLine(s);
		}

		var volumeInfoTableString = new StringBuilder();
		foreach (var entry in VolumeInfoTable.Entries)
		{
			volumeInfoTableString.Append($"0x{entry.Size.ToString("X", formatProvider)}".PadRight(20));
			volumeInfoTableString.Append($"0x{entry.NameOffset.ToString("X", formatProvider)}".PadRight(20));
			volumeInfoTableString.Append($"0x{entry.DataOffset.ToString("X", formatProvider)}".PadRight(20));
			volumeInfoTableString.AppendLine();
		}

		var headerPart6String = new StringBuilder();
		foreach (var entry in WriteableEntryTable.Entries)
		{
			headerPart6String.Append($"0x{entry.Volume.ToString("X", formatProvider)}".PadRight(20));
			headerPart6String.Append($"0x{entry.Flags.ToString("X", formatProvider)}".PadRight(20));
			headerPart6String.AppendLine();
		}

		var headerPart7String = new StringBuilder();
		foreach (var entry in WriteableSharedEntryInfoTable.Entries)
		{
			headerPart7String.Append($"0x{entry.NextSibling.ToString("X", formatProvider)}".PadRight(20));
			headerPart7String.Append($"0x{entry.PatchedEntry.ToString("X", formatProvider)}".PadRight(20));
			headerPart7String.AppendLine();
		}

		var headerPart8String = new StringBuilder();
		foreach (var hash in HashDigestTable.Entries)
		{
			headerPart8String.Append(hash.Data);
			headerPart8String.AppendLine();
		}

		return $"""
		        General Info
		        -----------------------------------------------------------
		        Archive Size:               {VolumeInfoTable.Entries.First().Size.ToString("X", formatProvider)}
		        Is Header Encrypted?        {WriterSettings.IsEncrypted}

		        Header size:                {Intro.TocSize.ToString("X", formatProvider)}

		        Header Intro
		        -----------------------------------------------------------
		        Magic Number:               {Intro.Magic.ToString("X", formatProvider)}
		        Expected SHA-256 hash:      {Intro.Hash}
		        AES 256 key hex string:     {Intro.AesKey}
		        Header size:                {Intro.TocSize.ToString("X", formatProvider)}
		        NeFS version:               {Intro.Version.ToString("X", formatProvider)}
		        Number of items:            {Intro.NumEntries.ToString("X", formatProvider)}
		        User Value:                 {Intro.UserValue.ToString("X", formatProvider)}
		        Random Padding:             {Convert.ToHexString(Intro.RandomPadding)}
		        Unused:                     {Intro.Unused.ToString("X", formatProvider)}

		        Header Table of Contents
		        -----------------------------------------------------------
		        Offset to Part 1:           {TableOfContents.EntryTableStart.ToString("X", formatProvider)}
		        Offset to Part 2:           {TableOfContents.SharedEntryInfoTableStart.ToString("X", formatProvider)}
		        Offset to Part 3:           {TableOfContents.NameTableStart.ToString("X", formatProvider)}
		        Offset to Part 4:           {TableOfContents.BlockTableStart.ToString("X", formatProvider)}
		        Offset to Part 5:           {TableOfContents.VolumeInfoTableStart.ToString("X", formatProvider)}
		        Offset to Part 6:           {TableOfContents.WritableEntryTableStart.ToString("X", formatProvider)}
		        Offset to Part 7:           {TableOfContents.WritableSharedEntryInfoTableStart.ToString("X", formatProvider)}
		        Offset to Part 8:           {TableOfContents.HashDigestTableStart.ToString("X", formatProvider)}
		        Num Volumes:                {TableOfContents.NumVolumes.ToString("X", formatProvider)}
		        Hash Block Size (<< 15):    {TableOfContents.HashBlockSize.ToString("X", formatProvider)}
		        Block Size (<< 15):         {TableOfContents.BlockSize.ToString("X", formatProvider)}
		        Split Size (<< 15):         {TableOfContents.SplitSize.ToString("X", formatProvider)}
		        Random Padding:             {Convert.ToHexString(TableOfContents.RandomPadding)}

		        Entry Table
		        -----------------------------------------------------------
		        Data Offset        Shared Entry        First Block        Id
		        {headerPart1String}
		        Shared Entry Info Table
		        -----------------------------------------------------------
		        Directory Id        First child Id        Name offset        Extracted size        Id
		        {headerPart2String}
		        Name Table
		        -----------------------------------------------------------
		        {headerPart3String}
		        Block Table
		        -----------------------------------------------------------
		        Number of entries:          {BlockTable.Entries.Count.ToString("X", formatProvider)}

		        Volume Info Table (Count: {VolumeInfoTable.Entries.Count})
		        -----------------------------------------------------------
		        Size        Name Offset        Data Offset
		        {volumeInfoTableString}
		        Writeable Entry Table (Count: {WriteableEntryTable.Entries.Count})
		        -----------------------------------------------------------
		        {headerPart6String}
		        Writeable Shared Entry Info Table (Count: {WriteableSharedEntryInfoTable.Entries.Count})
		        -----------------------------------------------------------
		        {headerPart7String}
		        Hash Digest Table (Count: {HashDigestTable.Entries.Count})
		        -----------------------------------------------------------
		        {headerPart8String}
		        """;
	}
}
