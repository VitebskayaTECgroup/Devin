﻿using Devin.Models;
using LinqToDB;
using System.Collections.Generic;
using System.Linq;

namespace Devin.ViewModels
{
    public class DevicesViewModel
    {
        public List<Folder> Folders { get; set; } = new List<Folder>();

        public List<Computer> Computers { get; set; } = new List<Computer>();

        public List<Device> Devices { get; set; } = new List<Device>();

        public DevicesViewModel(string Search = "")
        {
            using (var db = new DbDevin())
            {
                if (!string.IsNullOrEmpty(Search))
                {
                    var query = from d in db.Devices
                                from f in db.Folders.Where(x => x.Id == d.FolderId).DefaultIfEmpty()
                                from p in db.WorkPlaces.Where(x => x.Id == d.PlaceId).DefaultIfEmpty()
                                where !d.IsDeleted && (
                                    d.Inventory.Contains(Search)
                                    || d.Type.Contains(Search)
                                    || d.Name.Contains(Search)
                                    || d.PublicName.Contains(Search)
                                    || d.Description.Contains(Search)
                                    || d.Mol.Contains(Search)
                                    || d.SerialNumber.Contains(Search)
                                    || d.PassportNumber.Contains(Search)
                                    || d.Location.Contains(Search)
                                )
                                orderby d.Name, d.Type
                                select new Device {
                                    Id = d.Id,
                                    Type = d.Type,
                                    Inventory = d.Inventory,
                                    Name = d.Name,
                                    ComputerId = d.ComputerId,
                                    PublicName = d.PublicName,
                                    Mol = d.Mol,
                                    Location = p.Location,
                                    IsOff = d.IsOff,
                                    FolderId = f.Id
                                };

                    Devices = query.ToList();
                }
                else
                {
                    var devicesQuery = from d in db.Devices
                                       from f in db.Folders.Where(x => x.Id == d.FolderId).DefaultIfEmpty()
                                       from p in db.WorkPlaces.Where(x => x.Id == d.PlaceId).DefaultIfEmpty()
                                       where !d.IsDeleted
                                       orderby d.Name, d.Type
                                       select new Device
                                       {
                                           Id = d.Id,
                                           Type = d.Type,
                                           Inventory = d.Inventory,
                                           Name = d.Name,
                                           ComputerId = d.ComputerId,
                                           PublicName = d.PublicName,
                                           Mol = d.Mol,
                                           Location = p.Location,
                                           IsOff = d.IsOff,
                                           FolderId = f.Id
                                       };

                    var foldersQuery = from f in db.Folders
                                       from p in db.Folders.Where(x => x.Id == f.FolderId).DefaultIfEmpty(new Folder { Id = 0 })
                                       where f.Type == "device"
                                       orderby f.Name
                                       select new Folder
                                       {
                                           Id = f.Id,
                                           Name = f.Name,
                                           FolderId = p.Id,
                                       };

                    var __devices = devicesQuery.ToList();
                    var _folders = foldersQuery.ToList();

                    var _devices = new List<Device>();
                    var _computers = new List<Computer>();

                    foreach (var d in __devices)
                    {
                        if (d.Type == "CMP")
                        {
                            _computers.Add(new Computer
                            {
                                Id = d.Id,
                                Type = d.Type,
                                Inventory = d.Inventory,
                                Name = d.Name,
                                PublicName = d.PublicName,
                                Mol = d.Mol,
                                Location = d.Location,
                                IsOff = d.IsOff,
                                FolderId = d.FolderId
                            });
                        }
                        else
                        {
                            _devices.Add(d);
                        }
                    }

                    bool found = false;

                    foreach (var d in _devices)
                    {
                        found = false;
                        foreach (Computer computer in _computers)
                        {
                            if (d.ComputerId == computer.Id)
                            {
                                computer.Devices.Add(d);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            foreach (var folder in _folders)
                            {
                                if (d.FolderId == folder.Id)
                                {
                                    folder.Devices.Add(d);
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                Devices.Add(d);
                            }
                        }
                    }

                    foreach (var computer in _computers)
                    {
                        found = false;
                        foreach (Folder folder in _folders)
                        {
                            if (computer.FolderId == folder.Id)
                            {
                                folder.Computers.Add(computer);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            Computers.Add(computer);
                        }
                    }

                    foreach (var folder in _folders)
                    {
                        if (folder.FolderId == 0)
                        {
                            Folders.Add(Folder.Build(folder, _folders));
                        }
                    }
                }
            }
        }
    }
}