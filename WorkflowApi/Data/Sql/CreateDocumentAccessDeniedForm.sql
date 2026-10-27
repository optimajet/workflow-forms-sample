IF NOT EXISTS (
    SELECT 1
    FROM dbo.WorkflowForm
    WHERE Name = N'DocumentAccessDenied'
      AND Version = 0
      AND TenantId IS NULL
)
BEGIN
    INSERT INTO dbo.WorkflowForm (Id, Name, Version, CreationDate, UpdatedDate, Definition, Lock, TenantId)
    VALUES (NEWID(), N'DocumentAccessDenied', 0, GETDATE(), GETDATE(), N'{
  "version": "1",
  "form": {
    "key": "Screen",
    "type": "Screen",
    "props": {},
    "children": [
      {
        "key": "documentAccessDeniedMessage",
        "type": "RsHeader",
        "props": {
          "content": {
            "value": "This document is not available to the selected user."
          }
        }
      }
    ]
  },
  "localization": {},
  "languages": [
    {
      "code": "en",
      "dialect": "US",
      "name": "English",
      "description": "American English",
      "bidi": "ltr"
    }
  ],
  "defaultLanguage": "en-US"
}', 0, NULL);
END;
