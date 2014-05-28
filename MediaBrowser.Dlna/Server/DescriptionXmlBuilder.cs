﻿using MediaBrowser.Dlna.Common;
using MediaBrowser.Model.Dlna;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security;
using System.Text;

namespace MediaBrowser.Dlna.Server
{
    public class DescriptionXmlBuilder
    {
        private readonly DeviceProfile _profile;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly string _serverUdn;
        private readonly string _serverAddress;

        public DescriptionXmlBuilder(DeviceProfile profile, string serverUdn, string serverAddress)
        {
            if (string.IsNullOrWhiteSpace(serverUdn))
            {
                throw new ArgumentNullException("serverUdn");
            }

            _profile = profile;
            _serverUdn = serverUdn;
            _serverAddress = serverAddress;
        }

        public string GetXml()
        {
            var builder = new StringBuilder();

            builder.Append("<?xml version=\"1.0\"?>");

            builder.Append("<root xmlns=\"urn:schemas-upnp-org:device-1-0\" xmlns:dlna=\"urn:schemas-dlna-org:device-1-0\"");
            foreach (var att in _profile.XmlRootAttributes)
            {
                builder.AppendFormat(" {0}=\"{1}\"", att.Name, att.Value);
            }
            builder.Append(">");

            builder.Append("<specVersion>");
            builder.Append("<major>1</major>");
            builder.Append("<minor>0</minor>");
            builder.Append("</specVersion>");

            AppendDeviceInfo(builder);

            builder.Append("</root>");

            return builder.ToString();
        }

        private void AppendDeviceInfo(StringBuilder builder)
        {
            builder.Append("<device>");
            AppendDeviceProperties(builder);

            AppendIconList(builder);
            AppendServiceList(builder);
            builder.Append("</device>");
        }

        private void AppendDeviceProperties(StringBuilder builder)
        {
            builder.Append("<UDN>uuid:" + SecurityElement.Escape(_serverUdn) + "</UDN>");
            builder.Append("<dlna:X_DLNACAP>" + SecurityElement.Escape(_profile.XDlnaCap ?? string.Empty) + "</dlna:X_DLNACAP>");

            builder.Append("<dlna:X_DLNADOC xmlns:dlna=\"urn:schemas-dlna-org:device-1-0\">M-DMS-1.50</dlna:X_DLNADOC>");
            builder.Append("<dlna:X_DLNADOC xmlns:dlna=\"urn:schemas-dlna-org:device-1-0\">" + SecurityElement.Escape(_profile.XDlnaDoc ?? string.Empty) + "</dlna:X_DLNADOC>");

            builder.Append("<friendlyName>" + SecurityElement.Escape(_profile.FriendlyName ?? string.Empty) + "</friendlyName>");
            builder.Append("<deviceType>urn:schemas-upnp-org:device:MediaServer:1</deviceType>");
            builder.Append("<manufacturer>" + SecurityElement.Escape(_profile.Manufacturer ?? string.Empty) + "</manufacturer>");
            builder.Append("<manufacturerURL>" + SecurityElement.Escape(_profile.ManufacturerUrl ?? string.Empty) + "</manufacturerURL>");
            builder.Append("<modelName>" + SecurityElement.Escape(_profile.ModelName ?? string.Empty) + "</modelName>");
            builder.Append("<modelDescription>" + SecurityElement.Escape(_profile.ModelDescription ?? string.Empty) + "</modelDescription>");
            builder.Append("<modelNumber>" + SecurityElement.Escape(_profile.ModelNumber ?? string.Empty) + "</modelNumber>");
            builder.Append("<modelURL>" + SecurityElement.Escape(_profile.ModelUrl ?? string.Empty) + "</modelURL>");
            builder.Append("<serialNumber>" + SecurityElement.Escape(_profile.SerialNumber ?? string.Empty) + "</serialNumber>");

            //builder.Append("<URLBase>" + SecurityElement.Escape(_serverAddress) + "</URLBase>");
            
            if (!string.IsNullOrWhiteSpace(_profile.SonyAggregationFlags))
            {
                builder.Append("<av:aggregationFlags xmlns:av=\"urn:schemas-sony-com:av\">" + SecurityElement.Escape(_profile.SonyAggregationFlags) + "</av:aggregationFlags>");
            }
        }

        private void AppendIconList(StringBuilder builder)
        {
            builder.Append("<iconList>");

            foreach (var icon in GetIcons())
            {
                builder.Append("<icon>");

                builder.Append("<mimetype>" + SecurityElement.Escape(icon.MimeType ?? string.Empty) + "</mimetype>");
                builder.Append("<width>" + SecurityElement.Escape(icon.Width.ToString(_usCulture)) + "</width>");
                builder.Append("<height>" + SecurityElement.Escape(icon.Height.ToString(_usCulture)) + "</height>");
                builder.Append("<depth>" + SecurityElement.Escape(icon.Depth ?? string.Empty) + "</depth>");
                builder.Append("<url>" + SecurityElement.Escape(icon.Url ?? string.Empty) + "</url>");

                builder.Append("</icon>");
            }

            builder.Append("</iconList>");
        }

        private void AppendServiceList(StringBuilder builder)
        {
            builder.Append("<serviceList>");

            foreach (var service in GetServices())
            {
                builder.Append("<service>");

                builder.Append("<serviceType>" + SecurityElement.Escape(service.ServiceType ?? string.Empty) + "</serviceType>");
                builder.Append("<serviceId>" + SecurityElement.Escape(service.ServiceId ?? string.Empty) + "</serviceId>");
                builder.Append("<SCPDURL>" + SecurityElement.Escape(service.ScpdUrl ?? string.Empty) + "</SCPDURL>");
                builder.Append("<controlURL>" + SecurityElement.Escape(service.ControlUrl ?? string.Empty) + "</controlURL>");
                builder.Append("<eventSubURL>" + SecurityElement.Escape(service.EventSubUrl ?? string.Empty) + "</eventSubURL>");

                builder.Append("</service>");
            }

            builder.Append("</serviceList>");
        }

        private IEnumerable<DeviceIcon> GetIcons()
        {
            var list = new List<DeviceIcon>();

            list.Add(new DeviceIcon
            {
                MimeType = "image/png",
                Depth = "24",
                Width = 120,
                Height = 120,
                Url = "/mediabrowser/dlna/icons/logo120.png"
            });

            list.Add(new DeviceIcon
            {
                MimeType = "image/jpeg",
                Depth = "24",
                Width = 120,
                Height = 120,
                Url = "/mediabrowser/dlna/icons/logo120.jpg"
            });

            list.Add(new DeviceIcon
            {
                MimeType = "image/png",
                Depth = "24",
                Width = 48,
                Height = 48,
                Url = "/mediabrowser/dlna/icons/logo48.png"
            });

            list.Add(new DeviceIcon
            {
                MimeType = "image/jpeg",
                Depth = "24",
                Width = 48,
                Height = 48,
                Url = "/mediabrowser/dlna/icons/logo48.jpg"
            });

            return list;
        }

        private IEnumerable<DeviceService> GetServices()
        {
            var list = new List<DeviceService>();

            list.Add(new DeviceService
            {
                ServiceType = "urn:schemas-upnp-org:service:ContentDirectory:1",
                ServiceId = "urn:upnp-org:serviceId:ContentDirectory",
                ScpdUrl = "/mediabrowser/dlna/contentdirectory/contentdirectory.xml",
                ControlUrl = "/mediabrowser/dlna/contentdirectory/" + _serverUdn + "/control",
                EventSubUrl = "/mediabrowser/dlna/contentdirectory/" + _serverUdn + "/events"
            });

            list.Add(new DeviceService
            {
                ServiceType = "urn:schemas-upnp-org:service:ConnectionManager:1",
                ServiceId = "urn:upnp-org:serviceId:ConnectionManager",
                ScpdUrl = "/mediabrowser/dlna/connectionmanager/connectionmanager.xml",
                ControlUrl = "/mediabrowser/dlna/connectionmanager/" + _serverUdn + "/control",
                EventSubUrl = "/mediabrowser/dlna/connectionmanager/" + _serverUdn + "/events"
            });

            return list;
        }

        public override string ToString()
        {
            return GetXml();
        }
    }
}
