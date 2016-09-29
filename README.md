## WITSML Studio
The "PDS.Witsml.Studio" solution builds PDS WITSML Studio, a Windows desktop application written in C# and WPF using plug-in technology that can connect to any WITSML server via SOAP or ETP. It contains the following projects: 

##### PDS.Witsml.Studio
Provides the main application user interface for PDS WITSML Studio.

##### PDS.Witsml.Core
A collection of reusable components and plug-in framework.

##### PDS.Witsml.Studio.IntegrationTest
Contains integration tests for the WITSML Browser plug-in and core functionality.

##### PDS.Witsml.Studio.DataReplay
Data Producer plug-in that simulates streaming data in and out of a WITSML server.

##### PDS.Witsml.Studio.EtpBrowser
ETP Browser plug-in to communicate with a WITSML server via ETP protocol.

##### PDS.Witsml.Studio.ObjectInspector
Object Inspector plug-in that displays WITSML data objects with corresponding Energistics schema information.

##### PDS.Witsml.Studio.WitsmlBrowser
WITSML Browser plug-in to communicate with a WITSML server via SOAP.

##### PDS.Witsml.Studio.UnitTest
Unit tests for the WITSML Browser and core functionality.

---

### Copyright and License
Copyright &copy; 2016 Petrotechnical Data Systems

Released under the Apache License, Version 2.0