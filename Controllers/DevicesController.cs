﻿using Devin.Models;
using Devin.ViewModels;
using LinqToDB;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Devin.Controllers
{
    public class DevicesController : Controller
    {
        public ActionResult Index() => View();

        public ActionResult Load(string Item, string Search, string Sort)
        {
            if ((Item ?? "").Contains("device"))
            {
                int Id = int.Parse(Item.Replace("device", ""));
                Computer computer = new Computer { Id = Id };
                computer.Load();
                return View("ComputerData", computer);
            }
            else
            {
                var model = new DevicesViewModel(Search, Sort);

                if ((Item ?? "").Contains("folder"))
                {
                    int Id = int.Parse(Item.Replace("folder", ""));
                    return View("FolderData", Folder.FindSubFolder(model.Folders, Id));
                }
                else if (!string.IsNullOrEmpty(Search))
                {
                    ViewBag.Search = Search;
                    return View("Search", model.Devices);
                }
                else
                {
                    return View("List", model);
                }
            }
        }

        public ActionResult Cart(int Id) => View(model: Id);

        public ActionResult History() => View();

        public ActionResult Logs() => View();

        public ActionResult Files(string Id) => View(model: Id);

        public ActionResult HistoryById(string Id) => View(model: Id);

        public ActionResult ElmById(string Id) => View(model: Id);

        public ActionResult HistoryRepairsById(int Id) => View(model: Id);

        public ActionResult DefectAct(int Id) => View(model: Id);

        public ActionResult Users(int Id) => View("CardPartials/Users", model: Id);

        public ActionResult Table() => View();

        public ActionResult Table1C() => View();

        public ActionResult Compare1C() => View();

        public ActionResult WorkPlaces() => View();

        public ActionResult Repair(int Id) => View(model: Id);

        public ActionResult Inventory() => View();


        public JsonResult Update(
            [Bind(Include = "Id,Name,Inventory,Type,Description,PublicName,Location,PlaceId,Mol,SerialNumber,PassportNumber,ServiceTag,OS,OSKey,PrinterId,IsOff")] Device device,
            string DateInstall
        )
        {
            if (!DateTime.TryParse(DateInstall, out DateTime d))
            {
                return Json(new { Error = "Дата установки введена в неверном формате" });
            }
            else
            {
                device.DateInstall = d;
            }

            if (DateTime.TryParse(Request.Form.Get("DateLastRepair"), out d))
            {
                device.DateLastRepair = d;
            }
            else
            {
                device.DateLastRepair = null;
            }

            using (var db = new DevinContext())
            {
                var old = db.Devices.Where(x => x.Id == device.Id).FirstOrDefault();

                var changes = new List<string>();

                if (device.Inventory != old.Inventory) changes.Add($"инвентарный номер [{old.Inventory} => {device.Inventory}]");
                if (device.Name != old.Name) changes.Add($"наименование [{old.Name} => {device.Name}]");
                if (device.Type != old.Type) changes.Add($"класс [{old.Type} => {device.Type}]");
                if (device.Description != old.Description)  changes.Add($"описание [{old.Description} => {device.Description}]");
                if (device.Location != old.Location) changes.Add($"расположение [{old.Location} => {device.Location}]");
                if (device.PlaceId != old.PlaceId) changes.Add($"помещение [{old.PlaceId} => {device.PlaceId}]");
                if (device.Mol != old.Mol) changes.Add($"МОЛ [{old.Mol} => {device.Mol}]");
                if (device.SerialNumber != old.SerialNumber) changes.Add($"серийный номер [{old.SerialNumber} => {device.SerialNumber}]");
                if (device.PassportNumber != old.PassportNumber) changes.Add($"паспортный номер [{old.PassportNumber} => {device.PassportNumber}]");
                if (device.ServiceTag != old.ServiceTag) changes.Add($"сервис-тег [{old.ServiceTag} => {device.ServiceTag}]");
                if (device.PrinterId != old.PrinterId) changes.Add($"типовой принтер [{old.PrinterId} => {device.PrinterId}]");
                if (device.IsOff != old.IsOff) changes.Add($"списан [{old.IsOff} => {device.IsOff}]");
                if (device.DateInstall != old.DateInstall) changes.Add($"дата установки [{old.DateInstall} => {device.DateInstall}]");
                if (device.DateLastRepair != old.DateLastRepair) changes.Add($"дата последнего ремонта [{old.DateLastRepair} => {device.DateLastRepair}]");
                if (device.PublicName != old.PublicName) changes.Add($"имя для печати [{old.PublicName} => {device.PublicName}]");

                string destination = Request.Form.Get("Destination");
                device.ComputerId = old.ComputerId;
                device.FolderId = old.FolderId;

                string oldDestination = old.ComputerId != 0 
                    ? ("(компьютер) " + old.ComputerId) 
                    : (old.FolderId != 0 
                        ? ("(папка) " + old.FolderId)
                        : "отдельно");

                if (destination.Contains("computer"))
                {
                    destination = destination.Replace("computer", "");
                    if (oldDestination != ("(компьютер) " + destination))
                    {
                        device.ComputerId = int.Parse(destination);
                        device.FolderId = 0;
                        changes.Add($"расположение [{oldDestination} => (компьютер) {destination}]");
                    }
                }
                else if (destination.Contains("folder"))
                {
                    int i = int.Parse(destination.Replace("folder", ""));
                    if (oldDestination != ("(папка) " + i))
                    {
                        device.ComputerId = 0;
                        device.FolderId = i;
                        changes.Add($"расположение [{oldDestination} => (папка) {i}]");
                    }
                }
                else
                {
                    if (oldDestination != "отдельно")
                    {
                        device.FolderId = 0;
                        device.ComputerId = 0;
                        changes.Add($"расположение [{oldDestination} => отдельно]");
                    }                    
                }

                if (changes.Count > 0)
                {
                    db.Devices
                        .Where(x => x.Id == device.Id)
                        .Set(x => x.Type, device.Type)
                        .Set(x => x.Inventory, device.Inventory)
                        .Set(x => x.Name, device.Name)
                        .Set(x => x.PublicName, device.PublicName)
                        .Set(x => x.Description, device.Description)
                        .Set(x => x.Location, device.Location)
                        .Set(x => x.ServiceTag, device.ServiceTag)
                        .Set(x => x.Mol, device.Mol)
                        .Set(x => x.SerialNumber, device.SerialNumber)
                        .Set(x => x.PassportNumber, device.PassportNumber)
                        .Set(x => x.DateInstall, device.DateInstall)
                        .Set(x => x.DateLastRepair, device.DateLastRepair)
                        .Set(x => x.ComputerId, device.ComputerId)
                        .Set(x => x.FolderId, device.FolderId)
                        .Set(x => x.PlaceId, device.PlaceId)
                        .Set(x => x.PrinterId, device.PrinterId)
                        .Set(x => x.IsOff, device.IsOff)
                        .Set(x => x.DateLastChange, DateTime.Now)
                        .Update();

                    db.Log(User, "devices", device.Id, "Позиция изменена. Изменения: " + changes.ToLog());

                    return Json(new { Good = "Позиция успешно обновлена!<br />Изменены поля:<br />" + changes.ToHtml() });
                }
                else
                {
                    return Json(new { Warning = "Изменений не было" });
                }
            }
        }

        public JsonResult Copy(string Id)
        {
            int id = int.Parse(Id.Replace("device", ""));
            using (var db = new DevinContext())
            {
                var device = db.Devices.Where(x => x.Id == id).FirstOrDefault();
                if (device == null)
                {
                    return Json(new { Error = "Устройство, которое необходимо скопировать, не найдено в базе данных" });
                }
                
                device.Name += " (копия)";
                device.DateLastChange = DateTime.Now;

                int newId = db.InsertWithInt32Identity(device);
                db.Log(User, "devices", newId, "Позиция скопирована из [device" + Id + "]");

                return Json(new { Id = "device" + newId, Good = "Карточка устройства успешно скопирована" });
            }
        }

        public JsonResult Create()
        {
            using (var db = new DevinContext())
            {
                int id = db.InsertWithInt32Identity(new Device
                {
                    Inventory = "000000",
                    Name = "Новое устройство",
                    Type = "ALL",
                    Description = "",
                    Description1C = "",
                    Location = "",
                    PublicName = "",
                    NetworkName = "",
                    PassportNumber = "",
                    SerialNumber = "",
                    ServiceTag = "",
                    Gold = "",
                    Silver = "",
                    MPG = "",
                    Palladium = "",
                    Platinum = "",
                    Mol = "",
                    Files = "",
                    DateInstall = DateTime.Now.Date,
                    DateLastRepair = null,
                    ComputerId = 0,
                    FolderId = 0,
                    PlaceId = 0,
                    PrinterId = 0,
                    IsDeleted = false,
                    IsOff = false,
                    DateLastChange = DateTime.Now,
                });

                db.Log(User, "devices", id, "Создано новое устройство");

                return Json(new { Id = "device" + id, Good = "Создано новое устройство" });
            }

        }

        public JsonResult Delete(string Id)
        {
            int id = int.Parse(Id.Replace("device", ""));

            using (var db = new DevinContext())
            {
                db.Devices.Delete(x => x.Id == id);
                db.Log(User, "devices", id, "Устройство удалено");

                return Json(new { Good = "Устройство удалено" });
            }
        }

        public JsonResult MoveSelected(string Devices, string Key)
        {
            var selectedIdentifiers = Devices.Split(new [] { ";;" }, StringSplitOptions.RemoveEmptyEntries);

            using (var db = new DevinContext())
            {
                int ComputerId = int.TryParse(Key.Replace("computer", ""), out int i) ? i : 0;
                int FolderId = int.TryParse(Key.Replace("folder", ""), out i) ? i : 0;

                string message = "Устройство размещено отдельно";
                if (Key.Contains("computer")) message = "Устройство перемещено в компьютер [device" + ComputerId + "]";
                if (Key.Contains("folder")) message = "Устройство перемещено в папку [folder" + FolderId + "]";

                foreach (string identifier in selectedIdentifiers)
                {
                    if (identifier.Contains("device"))
                    {
                        int id = int.TryParse(identifier.Replace("device", ""), out i) ? i : 0;
                        if (id != 0)
                        {
                            db.Devices
                                .Where(x => x.Id == id)
                                .Set(x => x.ComputerId, ComputerId)
                                .Set(x => x.FolderId, FolderId)
                                .Set(x => x.DateLastChange, DateTime.Now)
                                .Update();
                            db.Log(User, "devices", id, message);
                        }
                    }
                }
            }

            return Json(new { Good = "Все устройства успешно перемещены" });
        }

        public JsonResult Move(int Id, int FolderId)
        {
            using (var db = new DevinContext())
            {
                int folderId = db.Folders.Where(x => x.Id == Id).Select(x => x.FolderId).FirstOrDefault();
                string oldText = folderId == 0 ? "" : "из папки [folder" + folderId + "] ";
                string newText = FolderId == 0 ? "и размещен отдельно" : "в папку [folder" + FolderId + "]";

                db.Devices
                    .Where(x => x.Id == Id)
                    .Set(x => x.FolderId, FolderId)
                    .Set(x => x.DateLastChange, DateTime.Now)
                    .Update();
                db.Log(User, "devices", Id, "Компьютер успешно перемещен " + oldText + newText);

                return Json(new { Good = "Компьютер успешно перемещен" });
            }
        }

        public JsonResult PrintDefectAct(string Description, string Inventory, string Name, string Position, string Mol, string Time)
        {
            var keys = new Dictionary<string, string>
            {
                { "motherboard", "материнская плата (вздутие конденсаторов, отказ контроллеров)" },
                { "power", "блок питания (перегрев входных цепец)" },
                { "cpu", "процессор (повреждение внутренних цепей контроллера)" },
                { "hdd", "жесткий диск (разрушение поверхности пластин вследствие большого износа накопителя)" },
                { "ram", "ОЗУ (перегрев)" },
                { "videocard", "видеокарта (перегрев, ошибки контроллера, артефакты)" },
            };

            var works = new Dictionary<string, string>
            {
                { "motherboard", "Ремонт материнской платы" },
                {"power", "Ремонт блока питания" },
                { "cpu", "Ремонт процессора" },
                { "hdd", "Ремонт жесткого диска" },
                { "ram", "Ремонт ОЗУ" },
                { "videocard", "Ремонт видеокарты" },
            };

            var months = new [] { "января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря" };


            DateTime Date = DateTime.TryParse(Request.Form.Get("DateInstall"), out DateTime d) ? d : DateTime.Now;
            var Positions = Request.Form.Get("positions").Split(new [] { ";;" }, StringSplitOptions.RemoveEmptyEntries);

            if (!System.IO.File.Exists(Server.MapPath("../content/templates/defect.xls"))) return Json(new { Error = "Файла шаблона не существует либо путь к нему неправильно прописан в исходниках" });

            HSSFWorkbook book;
            using (var fs = new FileStream(Server.MapPath("../content/templates/defect.xls"), FileMode.Open, FileAccess.Read))
            {
                book = new HSSFWorkbook(fs);
            }

            var sheet = book.GetSheetAt(0);

            sheet.GetRow(27).GetCell(22).SetCellValue(Position);
            sheet.GetRow(27).GetCell(68).SetCellValue(Mol);
            sheet.GetRow(33).GetCell(0).SetCellValue(Description + " (" + Name + ") инв. № " + Inventory + " Cрок работы: " + Time + " часов");
            sheet.GetRow(35).GetCell(16).SetCellValue("произошел выход из строя следующих комплектующих:");

            sheet.GetRow(87).GetCell(2).SetCellValue(Date.Day);
            sheet.GetRow(87).GetCell(8).SetCellValue(months[Date.Month - 1]);
            sheet.GetRow(87).GetCell(32).SetCellValue(Date.Year);

            string defects = "";
            int i = 0;

            foreach (string position in Positions)
            {
                if (position != "")
                {
                    var param = position.Split(new [] { "::" }, StringSplitOptions.RemoveEmptyEntries);

                    if (defects != "") defects += ", ";
                    defects += keys.ContainsKey(param[0]) ? keys[param[0]] : param[0];

                    sheet.GetRow(73 + i).GetCell(5).SetCellValue(works.ContainsKey(param[0]) ? works[param[0]] : "Ремонт (" + param[0] + ")");
                    sheet.GetRow(73 + i).GetCell(70).SetCellValue(param[1]);
                    i++;
                }
            }

            sheet.GetRow(38).GetCell(0).SetCellValue(defects);

            using (var fs = new FileStream(Server.MapPath("../content/excels/defect.xls"), FileMode.OpenOrCreate, FileAccess.Write))
            {
                book.Write(fs);
            }

            return Json(new { Good = "Дефектный акт создан", Link = Url.Action("excels", "content") + "/defect.xls" });
        }

        public JsonResult PrintRecordCartByIp(string Ip)
        {
            var addr = IPAddress.Parse(Ip);
            var entry = Dns.GetHostEntry(addr);
            string name = entry.HostName.Substring(0, entry.HostName.IndexOf('.'));

            using (var db = new DevinContext())
            {
                int id = db.Devices.Where(x => x.Type == "CMP" && x.Name == name).Select(x => x.Id).FirstOrDefault();
                if (id == 0) return Json(new { Error = "Компьютер с именем \"" + name + "\" не найден в базе." });

                return PrintRecordCart(id.ToString());
            }
        }

        public JsonResult PrintRecordCart(string Id)
        {
            int id = int.TryParse(Id.Replace("device", ""), out int i) ? i : 0;
            if (id == 0) return Json(new { Error = "Компьютер с идентификатором \"" + Id + "\" не найден в базе." });

            string template = Server.MapPath(Url.Action("templates", "content") + "/ReportCart.xls");

            HSSFWorkbook book;
            using (var fs = new FileStream(template, FileMode.Open, FileAccess.Read))
            {
                book = new HSSFWorkbook(fs);
            }

            var sheet = book.GetSheetAt(0);

            using (var db = new DevinContext())
            {
				var _cabinet = from d in db.Devices
							   from w in db.WorkPlaces.Where(x => x.Id == d.PlaceId).DefaultIfEmpty()
							   where d.Id == id
							   select w.Location;

                var cabinet = _cabinet.FirstOrDefault();

				var _devices = from d in db.Devices
							   from o in db.Objects1C.Where(x => x.Inventory == d.Inventory)
							   where (d.Id == id || d.ComputerId == id) && !d.IsDeleted
							   orderby d.Inventory, o.Description
							   select new
							   {
								   d.Inventory,
								   d.Description,
								   d.PublicName,
								   Description1C = o.Description ?? d.Description,
								   SerialNumber = d.SerialNumber ?? "",
								   d.DateInstall,
								   Mol = o.Mol ?? "",
							   };

                var devices = _devices.ToList();

                var name = db.Devices.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault();

                string now = DateTime.Now.ToString("dd.MM.yyyy");

				// Место расположения (хранения)
                sheet.GetRow(10).GetCell(7).SetCellValue(cabinet);

				// Дата составления
                sheet.GetRow(27).GetCell(0).SetCellValue("Дата составления: " + now);

                int step = 0;
                foreach (var device in devices)
                {
                    sheet.CopyRow(8, 9 + step);
                    IRow row = sheet.GetRow(9 + step);
                    row.GetCell(0).SetCellValue(device.Inventory);
                    row.GetCell(1).SetCellValue(device.Description1C);
                    row.GetCell(2).SetCellValue(device.SerialNumber);
                    row.GetCell(3).SetCellValue(device.PublicName);
                    row.GetCell(4).SetCellValue(device.DateInstall.ToString("dd.MM.yyyy"));
                    row.GetCell(5).SetCellValue(device.Mol);
                    row.GetCell(6).SetCellValue(now);
                    step++;
                }

                sheet.GetRow(8).Height = 0;
                string output = @"\\backup\pub\web\devin\Карточка_учета_оргтехники_" + name.Replace("/", "-").Replace(".", "") + ".xls";

                using (var fs = new FileStream(output, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    book.Write(fs);
                }

                return Json(new
                {
                    Good = "Карточка учета вычислительной техники на рабочем месте \"" + name + "\" создана",
                    Link = "http://www.vst.vitebsk.energo.net/files/devin/Карточка_учета_оргтехники_" + name.Replace("/", "-").Replace(".", "") + ".xls?r=" + (new Random()).Next()
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult PrintRecordCartByFolder(int Id)
        {
            string template = Server.MapPath(Url.Action("templates", "content") + "/ReportCart.xls");

            HSSFWorkbook book;
            using (var fs = new FileStream(template, FileMode.Open, FileAccess.Read))
            {
                book = new HSSFWorkbook(fs);
            }

            var sheet = book.GetSheetAt(0);

            using (var db = new DevinContext())
            {
                var cabinet = db.Folders.Where(x => x.Id == Id).FirstOrDefault();

                var devicesQuery = from d in db.Devices
                                   from c in db.Devices.Where(x => x.Type == "CMP" && x.Id == d.ComputerId).DefaultIfEmpty()
                                   from o in db.Objects1C.Where(x => x.Inventory == d.Inventory).DefaultIfEmpty()
                                   where (d.FolderId == Id || c.FolderId == Id) && !d.IsDeleted
                                   orderby d.Inventory, o.Description
                                   select new
                                   {
                                       Inventory = d.Inventory ?? "",
                                       Name = d.Name ?? "",
                                       Description = d.Description ?? "",
                                       PublicName = d.PublicName ?? "",
                                       SerialNumber = d.SerialNumber ?? "",
                                       d.DateInstall,
                                       Mol = o.Mol ?? "",
                                       Description1C = o.Description ?? d.Description ?? ""
                                   };

                var devices = devicesQuery.ToList().GroupBy(x => x).Select(x => x.Key).ToList();

                string now = DateTime.Now.ToString("dd.MM.yyyy");
                sheet.GetRow(4).GetCell(9).SetCellValue(cabinet.Name);
                sheet.GetRow(12).GetCell(0).SetCellValue(now);

                int step = 0;
                foreach (var device in devices)
                {
                    sheet.CopyRow(8, 9 + step);

                    var row = sheet.GetRow(9 + step);
                    row.GetCell(0).SetCellValue(device.Inventory);
                    row.GetCell(1).SetCellValue(device.Description1C ?? device.Description);
                    row.GetCell(2).SetCellValue(device.SerialNumber ?? "");
                    row.GetCell(3).SetCellValue(device.PublicName);
                    row.GetCell(4).SetCellValue(device.DateInstall.ToString("dd.MM.yyyy"));
                    row.GetCell(5).SetCellValue(device.Mol ?? "");
                    row.GetCell(6).SetCellValue(now);

                    step++;
                }

                sheet.GetRow(8).Height = 0;

                string output = @"\\backup\pub\web\devin\Карточка_учета_оргтехники_" + cabinet.Name.Replace("/", "-").Replace(".", "") + ".xls";

                using (var fs = new FileStream(output, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    book.Write(fs);
                }

                return Json(new
                {
                    Good = "Карточка учета вычислительной техники на рабочем месте \"" + cabinet.Name + "\" создана",
                    Link = "http://www.vst.vitebsk.energo.net/files/devin/Карточка_учета_оргтехники_" + cabinet.Name.Replace("/", "-").Replace(".", "") + ".xls?r=" + (new Random()).Next()
                }, JsonRequestBehavior.AllowGet);
            }

            
        }

        public JsonResult CreateWorkPlace()
        {
            using (var db = new DevinContext())
            {
                int id = db.WorkPlaces
                    .Value(x => x.Location, "")
                    .InsertWithInt32Identity() ?? 0;

                db.WorkPlaces
                    .Where(x => x.Id == id)
                    .Set(x => x.Location, "#" + id)
                    .Update();
                
                db.Log(User, "workplaces", id, "Создано новое рабочее место");

                return Json(new { Good = "Новое рабочее место создано" }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult UpdateWorkPlace([Bind(Include = "Id,Location,Guild")] WorkPlace place)
        {
            using (var db = new DevinContext())
            {
                var old = db.WorkPlaces.Where(x => x.Id == place.Id).FirstOrDefault();

                var changes = new List<string>();

                if (place.Location != old.Location) changes.Add("расположение [\"" + old.Location + "\" => \"" + place.Location + "\"]");
                if (place.Guild != old.Guild) changes.Add("подразделение [\"" + old.Guild + "\" => \"" + place.Guild + "\"]");

                if (changes.Count > 0)
                {
                    db.WorkPlaces
                        .Where(x => x.Id == place.Id)
                        .Set(x => x.Location, place.Location)
                        .Set(x => x.Guild, place.Guild)
                        .Update();
                    db.Log(User, "workplaces", place.Id, "Рабочее место изменено. Изменения: " + changes.ToLog());

                    return Json(new { Good = "Рабочее место изменено. Изменены поля:<br />" + changes.ToHtml() });
                }
                else
                {
                    return Json(new { Warning = "Изменений не было" });
                }
            }
        }

        public JsonResult DeleteWorkPlace(int Id)
        {
            using (var db = new DevinContext())
            {
                db.WorkPlaces.Delete(x => x.Id == Id);
                db.Log(User, "workplaces", Id, "Рабочее место удалено");
                return Json(new { Good = "Рабочее место удалено" });
            }
        }

        public JsonResult UploadFile(string Id)
        {
            int id = int.Parse(Id.Replace("device", ""));
            string root = @"\\web\DevinFiles\";
            string name = "";

            try
            {
                if (!Directory.Exists(root)) return Json(new { Error = "Папка-хранилище файлов \"" + root + "\" недоступна" });
                if (!Directory.Exists(root + "devices")) Directory.CreateDirectory(root + "devices");
                if (!Directory.Exists(root + "devices\\" + id)) Directory.CreateDirectory(root + "devices\\" + id);

                var file = Request.Files[0];
                name = file.FileName;

                if (System.IO.File.Exists(root + "devices\\" + id + "\\" + name)) return Json(new { Warning = "Файл с таким именем уже существует" });

                file.SaveAs(root + "devices\\" + id + "\\" + name);
            }
            catch (Exception)
            {
                return Json(new { Error = "Ошибка при доступе к файловой системе, возможно, нет прав у учетной записи ASP.NET" });
            }

            using (var db = new DevinContext())
            {
                string old = db.Devices.Where(x => x.Id == id).Select(x => x.Files).FirstOrDefault() ?? "";
                string changed = old + ";;" + name;

                db.Devices
                    .Where(x => x.Id == id)
                    .Set(x => x.Files, changed)
                    .Update();

                db.Log(User, "devices", id, "К списку файлов добавлен файл [" + name + "]");

                return Json(new { Good = "К списку файлов добавлен файл [" + name + "]" });
            }
        }

        public JsonResult DeleteFile(string Id, string File)
        {
            int id = int.Parse(Id.Replace("device", ""));

            System.IO.File.Delete(@"\\web\DevinFiles\devices\" + id + @"\" + File);

            using (var db = new DevinContext())
            {
                string old = db.Devices.Where(x => x.Id == id).Select(x => x.Files).FirstOrDefault() ?? "";
                string changed = old.Replace(File, "").Replace(";;;;", ";;");
                
                db.Devices
                    .Where(x => x.Id == id)
                    .Set(x => x.Files, changed)
                    .Update();

                db.Log(User, "devices", id, "Из списка файлов удален файл [" + File + "]");

                return Json(new { Good = "Из списка файлов удален файл [" + File + "]" });
            }
        }


        public JsonResult AddUser(int Id, string user)
        {
            using (var db = new DevinContext())
            {
                if (string.IsNullOrEmpty(user) || user == "0") return Json(new { Warning = "Пользователь не выбран" });

                var device = db.Devices
                    .Where(x => x.Id == Id)
                    .FirstOrDefault();

                if (device == null) return Json(new { Error = "Не найден принтер" });

                var users = (device.Users ?? "").Split(';').ToList();
                if (!users.Contains(user)) users.Add(user);

                db.Devices
                    .Where(x => x.Id == Id)
                    .Set(x => x.Users, string.Join(";", users.ToArray()))
                    .Update();

                db.Log(User, "devices", Id, "Пользователь \"" + user + "\" привязан к принтеру [device" + Id + "]");

                return Json(new { Good = "Пользователь привязан к принтеру" });
            }
        }

        public JsonResult RemoveUser(int Id, string user)
        {
            using (var db = new DevinContext())
            {
                if (string.IsNullOrEmpty(user) || user == "0") return Json(new { Warning = "Пользователь не выбран" });

                var device = db.Devices
                    .Where(x => x.Id == Id)
                    .FirstOrDefault();

                if (device == null) return Json(new { Error = "Не найден принтер" });

                var users = (device.Users ?? "").Split(';').ToList();
                if (users.Contains(user)) users.Remove(user);

                db.Devices
                    .Where(x => x.Id == Id)
                    .Set(x => x.Users, string.Join(";", users.ToArray()))
                    .Update();

                db.Log(User, "devices", Id, "Пользователь \"" + user + "\" отвязан от принтера [device" + Id + "]");

                return Json(new { Good = "Пользователь отвязан от принтера" });
            }
        }


        public JsonResult PrintDefect(int Id)
		{
            try
            {
                using (var db = new DevinContext())
                {
                    // Получение данных из базы
                    var today = DateTime.Today;
                    string[] months = { "января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря" };
                    var dateMark = today.ToString("MM") + "/" + today.ToString("dd");
                    var mark = db.WriteoffsMarks.FirstOrDefault(x => x.DateMark == dateMark);
                    if (mark == null)
                    {
                        mark = new WriteoffMark { DateMark = dateMark, Number = 1 };
                        db.Insert(mark);
                    }
                    else
                    {
                        mark.Number++;
                        db.WriteoffsMarks
                            .Where(x => x.DateMark == dateMark)
                            .Set(x => x.Number, mark.Number)
                            .Update();
                    }

                    var device = db.Devices.FirstOrDefault(x => x.Id == Id);
                    if (device == null) return Json(new { Warning = "" });

                    var _1c = db.Objects1C.FirstOrDefault(x => x.Inventory == device.Inventory);
                    if (_1c == null) return Json(new { Warning = "" });

                    var mol = _1c.Mol.Split(' ');
                    mol[0] = mol[0].Substring(0, 1).ToUpper() + mol[0].Substring(1).ToLower();


                    // Заполнение отчёта
                    HSSFWorkbook book;
                    using (var fs = new FileStream(Server.MapPath(Url.Action("templates", "content") + "\\def.xls"), FileMode.Open, FileAccess.Read))
                    {
                        book = new HSSFWorkbook(fs);
                    }

                    var sheet = book.GetSheetAt(2);

                    sheet.GetRow(26).GetCell(16).SetCellValue(dateMark + "-" + mark.Number);
                    sheet.GetRow(27).GetCell(16).SetCellValue("\"" + today.ToString("dd") + "\" " + months[today.Month - 1] + " " + today.ToString("yyyy") + " г.");
                    sheet.GetRow(28).GetCell(39).SetCellValue(today.ToString("yyyy") + " г.");

                    sheet.GetRow(30).GetCell(16).SetCellValue(_1c.Description);
                    sheet.GetRow(32).GetCell(16).SetCellValue(_1c.Inventory);
                    sheet.GetRow(33).GetCell(16).SetCellValue(device.SerialNumber);

                    sheet.GetRow(51).GetCell(27).SetCellValue(mol[0] + " " + mol[1]);
                    sheet.GetRow(53).GetCell(27).SetCellValue(mol[1] + " " + mol[0]);


                    // Сохранение отчета
                    sheet.ForceFormulaRecalculation = true;
                    HSSFFormulaEvaluator.EvaluateAllFormulaCells(book);
                    using (var fs = new FileStream(Server.MapPath(Url.Action("excels", "content") + "\\def.xls"), FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        book.Write(fs);
                    }

                    return Json(new
                    {
                        Good = "Дефектный акт создан",
                        Link = Url.Action("excels", "content") + "/def.xls",
                        Name = "Дефектный акт " + dateMark + "-" + mark.Number,
                    });
                }
            }
            catch (Exception e)
            {
                return Json(new { Error = e.Message });
            }
        }

        public JsonResult PrintOS(int Id)
        {
            try
            {
                using (var db = new DevinContext())
                {
                    // Получение данных из базы
                    var today = DateTime.Today;
                    string[] months = { "января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря" };
                    var dateMark = today.ToString("MM") + "/" + today.ToString("dd");
                    var mark = db.WriteoffsMarks.FirstOrDefault(x => x.DateMark == dateMark);
                    if (mark == null)
                    {
                        mark = new WriteoffMark { DateMark = dateMark, Number = 1 };
                        db.Insert(mark);
                    }
                    else
                    {
                        mark.Number++;
                        db.WriteoffsMarks
                            .Where(x => x.DateMark == dateMark)
                            .Set(x => x.Number, mark.Number)
                            .Update();
                    }

                    var device = db.Devices.FirstOrDefault(x => x.Id == Id);
                    if (device == null) return Json(new { Warning = "" });

                    var _1c = db.Objects1C.FirstOrDefault(x => x.Inventory == device.Inventory);
                    if (_1c == null) return Json(new { Warning = "" });

                    var mol = _1c.Mol.Split(' ');
                    mol[0] = mol[0].Substring(0, 1).ToUpper() + mol[0].Substring(1).ToLower();


                    // Заполнение отчёта
                    HSSFWorkbook book;
                    using (var fs = new FileStream(Server.MapPath(Url.Action("templates", "content") + "\\os.xls"), FileMode.Open, FileAccess.Read))
                    {
                        book = new HSSFWorkbook(fs);
                    }

                    var sheet = book.GetSheetAt(3);

                    sheet.GetRow(26).GetCell(16).SetCellValue(dateMark + "-" + mark.Number);
                    sheet.GetRow(27).GetCell(16).SetCellValue("\"" + today.ToString("dd") + "\" " + months[today.Month - 1] + " " + today.ToString("yyyy") + " г.");
                    sheet.GetRow(28).GetCell(16).SetCellValue(today.ToString("dd.MM.yyyy") + " г.");
                    sheet.GetRow(28).GetCell(39).SetCellValue(today.ToString("yyyy") + " г.");

                    sheet.GetRow(30).GetCell(16).SetCellValue(_1c.Description);
                    sheet.GetRow(32).GetCell(16).SetCellValue(_1c.Inventory);
                    sheet.GetRow(33).GetCell(16).SetCellValue(device.SerialNumber);

                    sheet.GetRow(51).GetCell(27).SetCellValue(mol[0] + " " + mol[1]);

                    sheet.GetRow(64).GetCell(59).SetCellValue(_1c.Gold);
                    sheet.GetRow(65).GetCell(59).SetCellValue(_1c.Silver);
                    sheet.GetRow(66).GetCell(59).SetCellValue(_1c.Platinum);
                    sheet.GetRow(67).GetCell(59).SetCellValue(_1c.Mpg);
                    sheet.GetRow(68).GetCell(59).SetCellValue(_1c.Palladium);


                    // стоимость драгметаллов из 1С
                    using (var site = new SiteContext())
                    {
                        var metals = site.MetalsCosts
                            .ToList()
                            .GroupBy(x => x.Name)
                            .Select(g => new
                            {
                                Name = g.Key.Trim(),
                                Cost = g
                                    .OrderByDescending(x => x.Date)
                                    .Select(x => x.Cost / 100 * (100 - x.Discount))
                                    .First()
                            })
                            .ToList();

                        sheet.GetRow(10).GetCell(16).SetCellValue(metals.FirstOrDefault(x => x.Name == "Золото")?.Cost ?? 0F);
                        sheet.GetRow(11).GetCell(16).SetCellValue(metals.FirstOrDefault(x => x.Name == "Серебро")?.Cost ?? 0F);
                        sheet.GetRow(12).GetCell(16).SetCellValue(metals.FirstOrDefault(x => x.Name == "Платина")?.Cost ?? 0F);
                        sheet.GetRow(13).GetCell(16).SetCellValue(metals.FirstOrDefault(x => x.Name == "Палладий")?.Cost ?? 0F); // МПГ
                        sheet.GetRow(14).GetCell(16).SetCellValue(metals.FirstOrDefault(x => x.Name == "Палладий")?.Cost ?? 0F);
                    }


                    // Сохранение отчета
                    sheet.ForceFormulaRecalculation = true;
                    HSSFFormulaEvaluator.EvaluateAllFormulaCells(book);
                    using (var fs = new FileStream(Server.MapPath(Url.Action("excels", "content") + "\\os.xls"), FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        book.Write(fs);
                    }

                    return Json(new
                    { 
                        Good = "Акты на списание ОС созданы",
                        Link = Url.Action("excels", "content") + "/os.xls",
                        Name = "Акты на списание ОС " + dateMark + "-" + mark.Number,
                    });
                }
            }
            catch (Exception e)
			{
                return Json(new { Error = e.Message });
			}
        }
    }
}