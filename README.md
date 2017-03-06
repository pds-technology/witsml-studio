## WITSML Studio

**Quick Links:**&nbsp;
[Blog](https://witsml.pds.technology/blog) |
[Getting Started](https://witsml.pds.technology/docs/getting-started) |
[Documentation](https://witsml.pds.technology/docs/documentation) |
[Downloads](https://witsml.pds.technology/docs/downloads) |
[Support](https://witsml.pds.technology/docs/support)

> **Note:** Be sure to perform a recursive clone of the repository to retrieve the `witsml` submodule.

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
Copyright &copy; 2017 Petrotechnical Data Systems

Released under the Apache License, Version 2.0

---

### Export Compliance

This source code makes use of cryptographic software:
- SSL/TLS is optionally used to secure web communications

The country in which you currently reside may have restrictions on the import, possession,
use, and/or re-export to another country, of encryption software.  BEFORE using any
encryption software, please check your country's laws, regulations and policies concerning
the import, possession, or use, and re-export of encryption software, to see if this is
permitted.  See <http://www.wassenaar.org/> for more information.

The U.S. Government Department of Commerce, Bureau of Industry and Security (BIS), has
classified this source code as Export Control Classification Number (ECCN) 5D002.c.1, which
includes information security software using or performing cryptographic functions with
symmetric and/or asymmetric algorithms.

This source code is published here:
> https://github.com/pds-technology/witsml-studio

In accordance with US Export Administration Regulations (EAR) Section 742.15(b), this
source code is not subject to EAR:
 - This source code has been made publicly available in accordance with EAR Section
   734.3(b)(3)(i) by publishing it in accordance with EAR Section 734.7(a)(4) at the above
   URL.
 - The BIS and the ENC Encryption Request Coordinator have been notified via e-mail of this
   URL.