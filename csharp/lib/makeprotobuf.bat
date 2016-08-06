REM This script is used to generate Phonemetadata.cs and Phonenumber.cs
REM from their protocol buffers definitions.

protogen -namespace=PhoneNumbers -cls_compliance=false --proto_path=..\..\resources ..\..\resources\phonemetadata.proto ..\..\resources\phonenumber.proto
python cleanprotobuf.py PhoneMetadata.cs ..\PhoneNumbers\PhoneMetadata.cs
python cleanprotobuf.py PhoneNumber.cs ..\PhoneNumbers\PhoneNumber.cs
