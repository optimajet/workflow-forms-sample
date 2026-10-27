BEGIN TRAN;

BEGIN TRY
DELETE
FROM dbo.WorkflowForm
WHERE Name IN (N'View', N'Rework', N'Created', N'Review', N'Delete');

INSERT INTO dbo.WorkflowForm (Id, Name, Version, CreationDate, UpdatedDate, Definition, Lock)
VALUES (NEWID(), N'T:Header', 0, GETDATE(), GETDATE(), N'{
  "version": "1",
  "form": {
    "key": "Screen",
    "type": "Screen",
    "props": {},
    "children": [
      {
        "key": "rsHeader1",
        "type": "RsHeader",
        "props": {
          "content": {
            "value": "Weekly Employee Report",
            "computeType": "function",
            "fnSource": "return `Weekly Employee Report : ${form.data.name ?? ''unnamed''} : ${form.data.stateName ?? ''state?''}`"
          }
        }
      },
      {
        "key": "rsContainer1",
        "type": "RsContainer",
        "props": {
          "readOnly": {
            "value": true
          }
        },
        "css": {
          "any": {
            "object": {
              "flexDirection": "row"
            }
          }
        },
        "children": [
          {
            "key": "submittedBy",
            "type": "RsInput",
            "props": {
              "readOnly": {
                "value": false
              },
              "label": {
                "value": "Submitted by"
              }
            }
          },
          {
            "key": "submittedOn",
            "type": "RsDatePicker",
            "props": {
              "readOnly": {
                "value": false
              },
              "label": {
                "value": "Submitted on"
              },
              "editable": {
                "value": true
              }
            }
          },
          {
            "key": "reviewedBy",
            "type": "RsInput",
            "props": {
              "readOnly": {
                "value": false
              },
              "label": {
                "value": "Reviewed by"
              }
            }
          }
        ]
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
}', 0),
       (NEWID(), N'T:EmployeeFields', 0, GETDATE(), GETDATE(), N'{
  "version": "1",
  "form": {
    "key": "Screen",
    "type": "Screen",
    "props": {},
    "children": [
      {
        "key": "whatWasDone",
        "type": "RsTextArea",
        "props": {
          "label": {
            "value": "What was done?"
          },
          "readOnly": {
            "value": false
          }
        },
        "schema": {
          "validations": []
        }
      },
      {
        "key": "encounteredProblems",
        "type": "RsTextArea",
        "props": {
          "label": {
            "value": "Encountered problems"
          },
          "readOnly": {
            "value": false
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
}', 0),
       (NEWID(), N'T:ManagerFields', 0, GETDATE(), GETDATE(), N'{
  "version": "1",
  "form": {
    "key": "Screen",
    "type": "Screen",
    "props": {},
    "children": [
      {
        "key": "managerReview",
        "type": "RsTextArea",
        "props": {
          "label": {
            "value": "Manager review"
          },
          "readOnly": {
            "value": false
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
}', 0),
       (NEWID(), N'View', 0, GETDATE(), GETDATE(), N'{
  "version": "1",
  "form": {
    "key": "Screen",
    "type": "Screen",
    "props": {},
    "children": [
      {
        "key": "templateTHeader1",
        "type": "Template:T:Header",
        "props": {}
      },
      {
        "key": "templateTEmployeeFields1",
        "type": "Template:T:EmployeeFields",
        "props": {
          "readOnly": {
            "value": true
          }
        }
      },
      {
        "key": "templateTManagerFields1",
        "type": "Template:T:ManagerFields",
        "props": {
          "readOnly": {
            "value": true
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
}', 0),
       (NEWID(), N'Rework', 0, GETDATE(), GETDATE(), N'{
  "version": "1",
  "form": {
    "key": "Screen",
    "type": "Screen",
    "props": {},
    "children": [
      {
        "key": "templateTHeader1",
        "type": "Template:T:Header",
        "props": {}
      },
      {
        "key": "templateTEmployeeFields1",
        "type": "Template:T:EmployeeFields",
        "props": {}
      },
      {
        "key": "templateTManagerFields1",
        "type": "Template:T:ManagerFields",
        "props": {
          "readOnly": {
            "value": true
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
}', 0),
       (NEWID(), N'Created', 0, GETDATE(), GETDATE(), N'{
  "version": "1",
  "form": {
    "key": "Screen",
    "type": "Screen",
    "props": {},
    "children": [
      {
        "key": "templateTHeader1",
        "type": "Template:T:Header",
        "props": {}
      },
      {
        "key": "templateTEmployeeFields1",
        "type": "Template:T:EmployeeFields",
        "props": {}
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
}', 0),
       (NEWID(), N'Review', 0, GETDATE(), GETDATE(), N'{
  "version": "1",
  "form": {
    "key": "Screen",
    "type": "Screen",
    "props": {},
    "children": [
      {
        "key": "templateTHeader1",
        "type": "Template:T:Header",
        "props": {}
      },
      {
        "key": "templateTEmployeeFields1",
        "type": "Template:T:EmployeeFields",
        "props": {
          "readOnly": {
            "value": true
          }
        }
      },
      {
        "key": "templateTManagerFields1",
        "type": "Template:T:ManagerFields",
        "props": {}
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
}', 0),
       (NEWID(), N'Delete', 0, GETDATE(), GETDATE(), N'{
  "version": "1",
  "form": {
    "key": "Screen",
    "type": "Screen",
    "props": {},
    "children": [
      {
        "key": "rsHeader1",
        "type": "RsHeader",
        "props": {
          "content": {
            "value": "You can delete the report you sent"
          }
        },
        "css": {
          "any": {
            "object": {
              "color": "rgba(186, 19, 19, 0.88)"
            }
          }
        }
      },
      {
        "key": "templateView1",
        "type": "Template:View",
        "props": {}
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
}', 0);

COMMIT TRAN;
END TRY
BEGIN CATCH
ROLLBACK TRAN;
    THROW;
END CATCH;