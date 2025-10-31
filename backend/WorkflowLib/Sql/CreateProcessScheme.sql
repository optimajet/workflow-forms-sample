MERGE INTO dbo.WorkflowScheme AS Target
    USING (SELECT 'WeeklyReportProcess' AS Code,
                  '<Process Name="WeeklyReportProcess" CanBeInlined="false" Tags="" LogEnabled="false">
                    <Designer />
                    <Actors>
                      <Actor Name="Author" Rule="Author" Value="" />
                      <Actor Name="CheckDivision" Rule="CheckDivision" Value="" />
                      <Actor Name="Manager" Rule="CheckRole" Value="Manager" />
                    </Actors>
                    <Commands>
                      <Command Name="Submit" />
                      <Command Name="Delete" />
                      <Command Name="Rework" />
                      <Command Name="Accept" />
                      <Command Name="Reject" />
                      <Command Name="Resubmit" />
                    </Commands>
                    <Activities>
                      <Activity Name="Created" State="Created" IsInitial="true" IsFinal="false" IsForSetState="true" IsAutoSchemeUpdate="true">
                        <Implementation>
                          <ActionRef Order="1" NameRef="InitReport" />
                          <ActionRef Order="2" NameRef="FormsPlugin.ShowForm">
                            <ActionParameter><![CDATA[{"AssociatedCommands":[{"SaveStrategy":"ValidateAndSave","CommandName":"Submit"}],"FormName":"Created","InParameterName":"WeeklyReport","DraftParameterName":"WeeklyReport","OutParameterName":"WeeklyReport"}]]></ActionParameter>
                          </ActionRef>
                        </Implementation>
                        <Designer X="320" Y="170" Hidden="false" />
                      </Activity>
                      <Activity Name="Review" State="Review" IsInitial="false" IsFinal="false" IsForSetState="true" IsAutoSchemeUpdate="true">
                        <Implementation>
                          <ActionRef Order="1" NameRef="FormsPlugin.ShowForm">
                            <ActionParameter><![CDATA[{"AssociatedCommands":[{"SaveStrategy":"ValidateAndSave","CommandName":"Rework"},{"SaveStrategy":"ValidateAndSave","CommandName":"Accept"},{"SaveStrategy":"ValidateAndSave","CommandName":"Reject"}],"FormName":"Review","InParameterName":"WeeklyReport","DraftParameterName":"WeeklyReport","OutParameterName":"WeeklyReport"}]]></ActionParameter>
                          </ActionRef>
                        </Implementation>
                        <Designer X="640" Y="170" Hidden="false" />
                      </Activity>
                      <Activity Name="Deleted" State="Deleted" IsInitial="false" IsFinal="true" IsForSetState="true" IsAutoSchemeUpdate="true">
                        <Designer X="910" Y="520" Hidden="false" />
                      </Activity>
                      <Activity Name="Accepted" State="Accepted" IsInitial="false" IsFinal="true" IsForSetState="true" IsAutoSchemeUpdate="true">
                        <Implementation>
                          <ActionRef Order="1" NameRef="SetReviewer" />
                        </Implementation>
                        <Designer X="910" Y="70" Hidden="false" />
                      </Activity>
                      <Activity Name="Rework" State="Rework" IsInitial="false" IsFinal="false" IsForSetState="true" IsAutoSchemeUpdate="true">
                        <Implementation>
                          <ActionRef Order="1" NameRef="SetReviewer" />
                          <ActionRef Order="2" NameRef="FormsPlugin.ShowForm">
                            <ActionParameter><![CDATA[{"AssociatedCommands":[{"SaveStrategy":"ValidateAndSave","CommandName":"Resubmit"}],"FormName":"Rework","InParameterName":"WeeklyReport","DraftParameterName":"WeeklyReport","OutParameterName":"WeeklyReport"}]]></ActionParameter>
                          </ActionRef>
                        </Implementation>
                        <Designer X="630" Y="400" Hidden="false" />
                      </Activity>
                      <Activity Name="Delete" State="Delete" IsInitial="false" IsFinal="false" IsForSetState="true" IsAutoSchemeUpdate="true">
                        <Implementation>
                          <ActionRef Order="1" NameRef="FormsPlugin.ShowForm">
                            <ActionParameter><![CDATA[{"AssociatedCommands":[{"SaveStrategy":"NoSave","CommandName":"Delete"}],"FormName":"Delete","InParameterName":"WeeklyReport","DraftParameterName":"WeeklyReport","OutParameterName":"WeeklyReport"}]]></ActionParameter>
                          </ActionRef>
                        </Implementation>
                        <Designer X="320" Y="520" Hidden="false" />
                      </Activity>
                      <Activity Name="Rejected" State="Rejected" IsInitial="false" IsFinal="true" IsForSetState="true" IsAutoSchemeUpdate="true">
                        <Implementation>
                          <ActionRef Order="1" NameRef="SetReviewer" />
                        </Implementation>
                        <Designer X="910" Y="250" Hidden="false" />
                      </Activity>
                    </Activities>
                    <Transitions>
                      <Transition Name="Created_Activity_1" To="Review" From="Created" Classifier="NotSpecified" AllowConcatenationType="And" RestrictConcatenationType="And" ConditionsConcatenationType="And" DisableParentStateControl="false">
                        <Restrictions>
                          <Restriction Type="Allow" NameRef="Author" />
                          <Restriction Type="Restrict" NameRef="Manager" />
                        </Restrictions>
                        <Triggers>
                          <Trigger Type="Command" NameRef="Submit" />
                        </Triggers>
                        <Conditions>
                          <Condition Type="Always" />
                        </Conditions>
                        <Designer Hidden="false" />
                      </Transition>
                      <Transition Name="Review_Created_1" To="Rework" From="Review" Classifier="NotSpecified" AllowConcatenationType="And" RestrictConcatenationType="And" ConditionsConcatenationType="And" DisableParentStateControl="false">
                        <Restrictions>
                          <Restriction Type="Allow" NameRef="CheckDivision" />
                          <Restriction Type="Allow" NameRef="Manager" />
                        </Restrictions>
                        <Triggers>
                          <Trigger Type="Command" NameRef="Rework" />
                        </Triggers>
                        <Conditions>
                          <Condition Type="Always" />
                        </Conditions>
                        <Designer X="667" Y="315" Hidden="false" />
                      </Transition>
                      <Transition Name="Review_Accepted_1" To="Accepted" From="Review" Classifier="NotSpecified" AllowConcatenationType="And" RestrictConcatenationType="And" ConditionsConcatenationType="And" DisableParentStateControl="false">
                        <Restrictions>
                          <Restriction Type="Allow" NameRef="CheckDivision" />
                          <Restriction Type="Allow" NameRef="Manager" />
                        </Restrictions>
                        <Triggers>
                          <Trigger Type="Command" NameRef="Accept" />
                        </Triggers>
                        <Conditions>
                          <Condition Type="Always" />
                        </Conditions>
                        <Designer Hidden="false" />
                      </Transition>
                      <Transition Name="Created_Activity_1_2" To="Delete" From="Created" Classifier="NotSpecified" AllowConcatenationType="And" RestrictConcatenationType="And" ConditionsConcatenationType="And" DisableParentStateControl="false" IsFork="true" SubprocessInOutDefinition="Start" SubprocessStartupType="SameThread" SubprocessStartupParameterCopyStrategy="CopyAll">
                        <Triggers>
                          <Trigger Type="Auto" />
                        </Triggers>
                        <Conditions>
                          <Condition Type="Always" />
                        </Conditions>
                        <Designer Hidden="false" />
                      </Transition>
                      <Transition Name="Activity_1_Deleted_1" To="Deleted" From="Delete" Classifier="NotSpecified" AllowConcatenationType="And" RestrictConcatenationType="And" ConditionsConcatenationType="And" DisableParentStateControl="false" IsFork="true" SubprocessInOutDefinition="Finalize" MergeViaSetState="true" SubprocessFinalizeParameterMergeStrategy="OverwriteAllNulls">
                        <Restrictions>
                          <Restriction Type="Allow" NameRef="Author" />
                        </Restrictions>
                        <Triggers>
                          <Trigger Type="Command" NameRef="Delete" />
                        </Triggers>
                        <Conditions>
                          <Condition Type="Action" NameRef="CanDelete" ConditionInversion="false" />
                        </Conditions>
                        <Designer Hidden="false" />
                      </Transition>
                      <Transition Name="Review_Rejected_1" To="Rejected" From="Review" Classifier="NotSpecified" AllowConcatenationType="And" RestrictConcatenationType="And" ConditionsConcatenationType="And" DisableParentStateControl="false">
                        <Restrictions>
                          <Restriction Type="Allow" NameRef="Manager" />
                          <Restriction Type="Allow" NameRef="CheckDivision" />
                        </Restrictions>
                        <Triggers>
                          <Trigger Type="Command" NameRef="Reject" />
                        </Triggers>
                        <Conditions>
                          <Condition Type="Always" />
                        </Conditions>
                        <Designer Hidden="false" />
                      </Transition>
                      <Transition Name="Rework_Review_1" To="Review" From="Rework" Classifier="NotSpecified" AllowConcatenationType="And" RestrictConcatenationType="And" ConditionsConcatenationType="And" DisableParentStateControl="false">
                        <Restrictions>
                          <Restriction Type="Allow" NameRef="Author" />
                        </Restrictions>
                        <Triggers>
                          <Trigger Type="Command" NameRef="Resubmit" />
                        </Triggers>
                        <Conditions>
                          <Condition Type="Always" />
                        </Conditions>
                        <Designer X="752" Y="315" Hidden="false" />
                      </Transition>
                      <Transition Name="Created_Accepted_1" To="Accepted" From="Created" Classifier="NotSpecified" AllowConcatenationType="And" RestrictConcatenationType="And" ConditionsConcatenationType="And" DisableParentStateControl="false">
                        <Restrictions>
                          <Restriction Type="Allow" NameRef="Author" />
                          <Restriction Type="Allow" NameRef="Manager" />
                        </Restrictions>
                        <Triggers>
                          <Trigger Type="Command" NameRef="Submit" />
                        </Triggers>
                        <Conditions>
                          <Condition Type="Always" />
                        </Conditions>
                        <Designer X="387" Y="96" Hidden="false" />
                      </Transition>
                    </Transitions>
                  </Process>'     AS Scheme,
                  0                     AS CanBeInlined,
                  NULL                  AS InlinedSchemes,
                  NULL                  AS Tags) AS Source
    ON Target.Code = Source.Code
    WHEN MATCHED THEN
        UPDATE
            SET Scheme         = Source.Scheme,
                CanBeInlined   = Source.CanBeInlined,
                InlinedSchemes = Source.InlinedSchemes,
                Tags           = Source.Tags
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (Code, Scheme, CanBeInlined, InlinedSchemes, Tags)
    VALUES (Source.Code, Source.Scheme, Source.CanBeInlined, Source.InlinedSchemes, Source.Tags);
