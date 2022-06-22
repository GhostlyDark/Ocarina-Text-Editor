﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;
using System.IO;
using OcarinaTextEditor.Enums;
using System.Windows;

namespace OcarinaTextEditor
{
    class Importer
    {
        private ObservableCollection<Message> m_messageList;
        private MemoryStream m_inputFile;

        public Importer()
        {
            m_messageList = new ObservableCollection<Message>();
        }

        public Importer(string fileName, EditMode Mode, bool Debug)
        {
            List<TableRecord> tableRecordList = new List<TableRecord>();
            TableRecord fuck = null;
            if (Mode == EditMode.ZZRPL)
            {
                try
                {
                    string zzrplFolder = Path.GetDirectoryName(fileName);
                    string msgData = Path.Combine(zzrplFolder, "messages", "StringData.bin");
                    string table = Path.Combine(zzrplFolder, "messages", "MessageTable.tbl");

                    using (FileStream stream = new FileStream(table, FileMode.Open, FileAccess.Read))
                    {
                        m_inputFile = new MemoryStream();
                        stream.CopyTo(m_inputFile);

                        EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);
                        reader.BaseStream.Seek(0, 0);

                        //Read in message table records
                        while (reader.BaseStream.Position != reader.BaseStream.Length && reader.PeekReadInt16() != -1)
                        {
                            TableRecord mesRecord = new TableRecord(reader);
                            tableRecordList.Add(mesRecord);
                        }
                    }

                    using (FileStream stream = new FileStream(msgData, FileMode.Open, FileAccess.Read))
                    {
                        m_messageList = new ObservableCollection<Message>();
                        EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);

                        foreach (var mesgRecord in tableRecordList)
                        {
                            fuck = mesgRecord;

                            if (mesgRecord.Offset >= reader.BaseStream.Length)
                                continue;

                            reader.BaseStream.Position = mesgRecord.Offset;
                            Message mes = new Message(reader, mesgRecord);
                            m_messageList.Add(mes);
                        }
                    }

                }
                catch (IOException ex)
                {
                    MessageBox.Show(ex.Message + " " + fuck.MessageID);
                    return;
                }
                catch (Exception)
                {
                    MessageBox.Show("Failed loading messages.");
                    return;
                }
            }
            else
            {
                long offset = Mode == EditMode.ZZRT ? 0x0012E4C0 : Debug ? 0x00BC24C0 : 0x00B849EC;
                long msgOffset = Mode == EditMode.ZZRT ? 0 : Debug ? 0x8C6000 : 0x92D000;

                string zzrpFolder = Path.GetDirectoryName(fileName);
                string codeFilePath = Path.Combine(zzrpFolder, "system", "code");
                string msgDataPath = Path.Combine(zzrpFolder, "misc", "nes_message_data_static");

                try
                {
                    using (FileStream stream = new FileStream(Mode == EditMode.ZZRT ? codeFilePath : fileName, FileMode.Open, FileAccess.Read))
                    {
                        m_inputFile = new MemoryStream();
                        stream.CopyTo(m_inputFile);

                        EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);
                        reader.BaseStream.Seek(offset, 0);

                        //Read in message table records
                        while (reader.PeekReadInt16() != -1)
                        {
                            TableRecord mesRecord = new TableRecord(reader);
                            tableRecordList.Add(mesRecord);
                        }
                    }

                    using (FileStream stream = new FileStream(Mode == EditMode.ZZRT ? msgDataPath : fileName, FileMode.Open, FileAccess.Read))
                    {
                        m_messageList = new ObservableCollection<Message>();
                        EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);

                        foreach (var mesgRecord in tableRecordList)
                        {
                            reader.BaseStream.Position = msgOffset + mesgRecord.Offset;
                            Message mes = new Message(reader, mesgRecord);
                            m_messageList.Add(mes);
                        }
                    }
                }
                catch (IOException ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
                catch (Exception)
                {
                    MessageBox.Show("Failed loading messages. Note: ROMs built by zzromtool are not supported directly!");
                    return;
                }
            }
        }

        public Importer(string tableFileName, string messageDataFileName)
        {
            m_messageList = new ObservableCollection<Message>();

            List<TableRecord> tableRecordList = new List<TableRecord>();

            //Read in message table records
            using (FileStream stream = new FileStream(tableFileName, FileMode.Open))
            {
                EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);

                while (reader.PeekReadInt16() != -1)
                {
                    TableRecord mesRecord = new TableRecord(reader);
                    tableRecordList.Add(mesRecord);
                }
            }

            //Read in message data
            using (FileStream stream = new FileStream(messageDataFileName, FileMode.Open))
            {
                EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);

                foreach (var mesgRecord in tableRecordList)
                {
                    reader.BaseStream.Position = mesgRecord.Offset;
                    Message mes = new Message(reader, mesgRecord);

                    m_messageList.Add(mes);
                }
            }
        }

        public ObservableCollection<Message> GetMessageList()
        {
            return m_messageList;
        }

        public MemoryStream GetInputFile()
        {
            return m_inputFile;
        }
    }
}
