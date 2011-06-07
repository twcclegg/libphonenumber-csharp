REM This script is used to generate Phonemetadata.cs and Phonenumber.cs
REM from their protocol buffers definitions.

protogen -namespace=PhoneNumbers -cls_compliance=false --proto_path=..\..\resources ..\..\resources\phonemetadata.proto ..\..\resources\phonenumber.proto
python cleanprotobuf.py Phonemetadata.cs ..\PhoneNumbers\Phonemetadata.cs
python cleanprotobuf.py Phonenumber.cs ..\PhoneNumbers\PhoneNumber.cs
