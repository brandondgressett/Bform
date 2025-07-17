# BFormDomain Component Catalog

This document provides a comprehensive catalog of all components in the BFormDomain project, organized by directory structure.

## Project Overview
- **Total C# Files**: 508
- **Root Path**: `/mnt/d/SystemLevel/SourceControl/sls/Codelab/Common/BFormDomain/CommonCode/`

## Directory Structure and Components

### 1. Diagnostics (`/Diagnostics/`)
**Purpose**: Application diagnostics, performance metrics, and alerts system

**Files**:
- `ApplicationAlertKind.cs` - Enumeration for alert types
- `FilePerformanceMetricPersistence.cs` - File-based persistence for performance metrics
- `FilePerformanceMetricPersistenceOptions.cs` - Configuration options for file persistence
- `IApplicationAlert.cs` - Interface for application alerts
- `IPerformanceMetricPersistence.cs` - Interface for metric persistence
- `PerfRateTrack.cs` - Performance rate tracking
- `PerfTrack.cs` - Performance tracking utility
- `PerformanceMetric.cs` - Performance metric model
- `PerformanceMetricReportingWorker.cs` - Background worker for metric reporting
- `SimpleApplicationAlert.cs` - Basic alert implementation
- `SwitchingApplicationAlert.cs` - Alert with switching capabilities
- `SwitchingApplicationAlertOptions.cs` - Configuration for switching alerts

**Key Components**:
- Performance tracking system
- Application alert framework
- Metric persistence layer

### 2. MessageBus (`/MessageBus/`)
**Purpose**: Message bus abstraction layer for distributed messaging

#### 2.1 AMQPInterfaces (`/MessageBus/AMQPInterfaces/`)
**Purpose**: AMQP protocol abstractions

**Files**:
- `ExchangeTypes.cs` - AMQP exchange type definitions
- `IExchangeSpecifier.cs` - Interface for exchange specification
- `IMessageAcknowledge.cs` - Message acknowledgment interface
- `IMessageBusSpecifier.cs` - Message bus specification interface
- `IMessageListener.cs` - Message listener interface
- `IMessagePublisher.cs` - Message publisher interface
- `IMessageRetriever.cs` - Message retriever interface
- `IQueueSpecifier.cs` - Queue specification interface
- `MessageBusTopology.cs` - Message bus topology configuration
- `MessageContext.cs` - Message context information
- `MessageExchangeDeclaration.cs` - Exchange declaration model
- `MessageQueueDeclaration.cs` - Queue declaration model
- `MessageQueueEnvelope.cs` - Message envelope wrapper

#### 2.2 InMemory (`/MessageBus/InMemory/`)
**Purpose**: In-memory message bus implementation for testing/development

**Files**:
- `IMemQueueAccess.cs` - Memory queue access interface
- `LightMessageQueueEnvelope.cs` - Lightweight message envelope
- `MemExchange.cs` - In-memory exchange implementation
- `MemMessageBus.cs` - In-memory message bus
- `MemMessageListener.cs` - In-memory listener
- `MemMessagePublisher.cs` - In-memory publisher
- `MemMessageRetriever.cs` - In-memory retriever
- `MemQueueAcknowledge.cs` - In-memory acknowledgment
- `MemQueueSpecifier.cs` - In-memory queue specifier

#### 2.3 RabbitMQ (`/MessageBus/RabbitMQ/`)
**Purpose**: RabbitMQ implementation (placeholder)

### 3. Platform (`/Platform/`)
**Purpose**: Core platform features and business logic

#### 3.1 AppEvents (`/Platform/AppEvents/`)
**Purpose**: Application event system for event-driven architecture

**Files**:
- `AppEvent.cs` - Application event model
- `AppEventBridge.cs` - Event bridge for cross-system events
- `AppEventConstants.cs` - Event-related constants
- `AppEventDistributer.cs` - Event distribution logic
- `AppEventOrigin.cs` - Event origin tracking
- `AppEventPump.cs` - Event pump for processing
- `AppEventPumpOptions.cs` - Event pump configuration
- `AppEventRepository.cs` - Event storage repository
- `AppEventRuleView.cs` - Rule view for events
- `AppEventSink.cs` - Event sink for consumption
- `AppEventSinkOptions.cs` - Event sink configuration
- `AppEventState.cs` - Event state management
- `EventPumpRoleSpecifier.cs` - Role specification for event pumps
- `IAppEventConsumer.cs` - Event consumer interface
- `IAppEventConsumerRegistrar.cs` - Consumer registration interface
- `TopicBinding.cs` - Topic binding configuration
- `TopicRegistrations.cs` - Topic registration management
- `UserActionCompletion.cs` - User action completion tracking

#### 3.2 ApplicationTopology (`/Platform/ApplicationTopology/`)
**Purpose**: Distributed application topology management

**Files**:
- `ApplicationServerMonitor.cs` - Server monitoring
- `ApplicationServerRecord.cs` - Server record model
- `ApplicationServerRepository.cs` - Server repository
- `ApplicationTopologyCatalog.cs` - Topology catalog
- `IServerRoleSpecifier.cs` - Server role interface
- `ServerRoleBalance.cs` - Server role balancing

#### 3.3 Authorization (`/Platform/Authorization/`)
**Purpose**: Authentication and authorization system

**Files**:
- `AppInitializationLogic.cs` - Application initialization
- `ApplicationRole.cs` - Role model
- `ApplicationUser.cs` - User model
- `ApplicationUserViewModel.cs` - User view model
- `AuthResponse.cs` - Authentication response
- `AuthorizationInitOptions.cs` - Authorization initialization options
- `CustomRoleManager.cs` - Custom role management
- `CustomSignInManager.cs` - Custom sign-in management
- `CustomUserManager.cs` - Custom user management
- `InvitationDataModel.cs` - User invitation model
- `InvitationLogic.cs` - Invitation business logic
- `InvitationLogicOptions.cs` - Invitation configuration
- `InvitationRepository.cs` - Invitation storage
- `JwtComponent.cs` - JWT token component
- `JwtConfig.cs` - JWT configuration
- `LoginLogic.cs` - Login business logic
- `PasswordHasher.cs` - Password hashing utility
- `RefreshToken.cs` - Refresh token model
- `RefreshTokenRepository.cs` - Refresh token storage
- `RegistrationLogic.cs` - User registration logic
- `RoleRepository.cs` - Role storage
- `TokenRequest.cs` - Token request model
- `UserInformationCache.cs` - User info caching
- `UserManagementLogic.cs` - User management logic
- `UserRepository.cs` - User storage
- `UserTagsDataModel.cs` - User tags model
- `UserTagsRepository.cs` - User tags storage

#### 3.4 Comments (`/Platform/Comments/`)
**Purpose**: Commenting system for entities

**Files**:
- `Comment.cs` - Comment model
- `CommentRepository.cs` - Comment storage
- `CommentViewModel.cs` - Comment view model
- `CommentsLogic.cs` - Comment business logic

**RuleActions**:
- `RuleActionCommentCreate.cs` - Create comment action
- `RuleActionCommentDelete.cs` - Delete comment action
- `RuleActionCommentEdit.cs` - Edit comment action

#### 3.5 Constants (`/Platform/Constants/`)
**Purpose**: System-wide constants

**Files**:
- `ApplicationRoles.cs` - Application role constants
- `BuiltIn.cs` - Built-in constants

#### 3.6 Content (`/Platform/Content/`)
**Purpose**: Content management system

**Files**:
- `ContentDomain.cs` - Content domain model
- `ContentElement.cs` - Content element model
- `FileApplicationPlatformContent.cs` - File-based content
- `FileApplicationPlatformContentOptions.cs` - File content options
- `IApplicationPlatformContent.cs` - Content interface
- `IContentDomainSource.cs` - Content source interface
- `IContentType.cs` - Content type interface
- `SatelliteJson.cs` - Satellite JSON handling

#### 3.7 Entity (`/Platform/Entity/`)
**Purpose**: Core entity framework

**Files**:
- `EntityReferenceLoader.cs` - Entity reference loading
- `EntityWrapping.cs` - Entity wrapper utilities
- `IAppEntity.cs` - Core entity interface
- `IEntityAttachment.cs` - Entity attachment interface
- `IEntityAttachmentManager.cs` - Attachment manager interface
- `IEntityInstanceLogic.cs` - Entity instance logic interface
- `IEntityLoaderModule.cs` - Entity loader module interface
- `IEntityReferenceBuilder.cs` - Reference builder interface
- `TemplateNamesCache.cs` - Template name caching

#### 3.8 ExceptionFunnel (`/Platform/ExceptionFunnel/`)
**Purpose**: Exception handling and UI error management

**Files**:
- `UIError.cs` - UI error model
- `UIErrorTemplate.cs` - Error template
- `UIExceptionFunnel.cs` - Exception funnel
- `UIExceptionFunnelRegistry.cs` - Funnel registry
- `UIExceptionFunnelTypes.cs` - Funnel type definitions

#### 3.9 Forms (`/Platform/Forms/`)
**Purpose**: Dynamic forms system

**Files**:
- `AcceptFormInstanceContent.cs` - Form content acceptance
- `ActionButton.cs` - Form action button
- `CreateFormInstancesCommand.cs` - Form creation command
- `FormEntityLoaderModule.cs` - Form entity loader
- `FormHelperLogic.cs` - Form helper utilities
- `FormInstance.cs` - Form instance model
- `FormInstanceHome.cs` - Form instance home
- `FormInstanceReferenceBuilderImplementation.cs` - Reference builder
- `FormInstanceRepository.cs` - Form storage
- `FormInstanceViewModel.cs` - Form view model
- `FormLogic.cs` - Form business logic
- `FormTemplate.cs` - Form template model
- `FormTemplateContentDomainSource.cs` - Form content source
- `FormTemplateViewModel.cs` - Form template view model
- `UpdateFormInstancesCommand.cs` - Form update command

**RuleActions**:
- `RuleActionCreateForm.cs` - Create form action
- `RuleActionCreateForms.cs` - Create multiple forms
- `RuleActionDeleteForm.cs` - Delete form action
- `RuleActionEditFormProperty.cs` - Edit form property
- `RuleActionFormEnrollDashboard.cs` - Enroll form in dashboard
- `RuleActionLookupForm.cs` - Lookup form action
- `RuleActionSetFormProperties.cs` - Set form properties
- `RuleActionTagForm.cs` - Tag form action
- `RuleActionUntagForm.cs` - Untag form action

#### 3.10 HtmlEntity (`/Platform/HtmlEntity/`)
**Purpose**: HTML content entities

**Files**:
- `HtmlEntityLoaderModule.cs` - HTML entity loader
- `HtmlEntityReferenceBuilder.cs` - HTML reference builder
- `HtmlInstance.cs` - HTML instance model
- `HtmlLogic.cs` - HTML business logic
- `HtmlTemplate.cs` - HTML template model
- `HtmlTemplateContentDomainSource.cs` - HTML content source

**RuleActions**:
- `RuleActionHtmlEnrollDashboard.cs` - Enroll HTML in dashboard

#### 3.11 KPIs (`/Platform/KPIs/`)
**Purpose**: Key Performance Indicators system

**Files**:
- `AcceptKPIInstanceContent.cs` - KPI content acceptance
- `CreateKPIInstanceCommand.cs` - KPI creation command
- `KPIComputeStage.cs` - KPI computation stages
- `KPIComputeType.cs` - KPI computation types
- `KPIData.cs` - KPI data model
- `KPIDataGroomingService.cs` - KPI data grooming
- `KPIDataRepository.cs` - KPI data storage
- `KPIEntityLoaderModule.cs` - KPI entity loader
- `KPIEvaluator.cs` - KPI evaluation logic
- `KPIInstance.cs` - KPI instance model
- `KPIInstanceReferenceBuilder.cs` - KPI reference builder
- `KPIInstanceRepository.cs` - KPI instance storage
- `KPIInsufficientDataException.cs` - KPI exception
- `KPILogic.cs` - KPI business logic
- `KPIMath.cs` - KPI mathematical operations
- `KPISample.cs` - KPI sample model
- `KPISampleViewModel.cs` - KPI sample view model
- `KPISamplesViewModel.cs` - KPI samples view model
- `KPISignal.cs` - KPI signal model
- `KPISignalStage.cs` - KPI signal stages
- `KPISignalType.cs` - KPI signal types
- `KPISignalViewModel.cs` - KPI signal view model
- `KPISignalsViewModel.cs` - KPI signals view model
- `KPISource.cs` - KPI data source
- `KPITemplate.cs` - KPI template model
- `KPITemplateContentDomainSource.cs` - KPI template source
- `KPITemplateViewModel.cs` - KPI template view model
- `KPIViewModel.cs` - KPI view model
- `ZScore.cs` - Z-score calculations

**RuleActions**:
- `RuleActionCreateKPI.cs` - Create KPI action
- `RuleActionDeleteKPI.cs` - Delete KPI action
- `RuleActionEvaluateKPIs.cs` - Evaluate KPIs action
- `RuleActionKPIEnrollDashboard.cs` - Enroll KPI in dashboard

#### 3.12 ManagedFile (`/Platform/ManagedFile/`)
**Purpose**: Managed file storage system

**Files**:
- `FileRecordRepository.cs` - File record storage
- `IManagedFilePersistence.cs` - File persistence interface
- `ManagedFileAudit.cs` - File audit model
- `ManagedFileAuditRepository.cs` - File audit storage
- `ManagedFileEntityLoaderModule.cs` - File entity loader
- `ManagedFileEvents.cs` - File-related events
- `ManagedFileGroomingService.cs` - File grooming service
- `ManagedFileInstance.cs` - File instance model
- `ManagedFileLogic.cs` - File business logic
- `ManagedFileReferenceBuilder.cs` - File reference builder
- `ManagedFileStoreOptions.cs` - File store options
- `ManagedFileViewModel.cs` - File view model
- `PhysicalFilePersistence.cs` - Physical file storage
- `PhysicalFilePersistenceOptions.cs` - Physical storage options

#### 3.13 Notification (`/Platform/Notification/`)
**Purpose**: Notification and messaging system

**Files**:
- `BuiltInNotificationGroups.cs` - Built-in notification groups
- `ChannelRegulation.cs` - Channel regulation rules
- `ChannelType.cs` - Notification channel types
- `ChannelsAllowed.cs` - Allowed channels configuration
- `ExecuteNotifyCommand.cs` - Notification command
- `INotificationCore.cs` - Notification core interface
- `IRegulatedNotificationLogic.cs` - Regulated notification interface
- `IWebNotificationSink.cs` - Web notification interface
- `NotificationAudit.cs` - Notification audit model
- `NotificationAuditEvent.cs` - Audit event model
- `NotificationAuditRepository.cs` - Audit storage
- `NotificationContact.cs` - Contact model
- `NotificationContactLogic.cs` - Contact logic
- `NotificationContactReference.cs` - Contact reference
- `NotificationContactRepository.cs` - Contact storage
- `NotificationGroup.cs` - Notification group model
- `NotificationGroupLogic.cs` - Group logic
- `NotificationGroupRepository.cs` - Group storage
- `NotificationMessage.cs` - Message model
- `NotificationService.cs` - Notification service
- `NotificationTimeSeverityTable.cs` - Time/severity mapping
- `RegulatedNotificationLogic.cs` - Regulated logic implementation
- `RegulatedNotificationOptions.cs` - Regulation options
- `RequestNotification.cs` - Notification request
- `TimeShift.cs` - Time shift model
- `TimeShifts.cs` - Time shifts collection
- `TwilioNotificationCore.cs` - Twilio integration
- `TwilioNotificationOptions.cs` - Twilio options
- `UserToast.cs` - User toast notification
- `UserToastLogic.cs` - Toast logic
- `UserToastRepository.cs` - Toast storage
- `WebNotification.cs` - Web notification model
- `WebNotificationKind.cs` - Web notification types

**RuleActions**:
- `RuleActionRequestNotification.cs` - Request notification action

#### 3.14 Reports (`/Platform/Reports/`)
**Purpose**: Reporting system

**Files**:
- `AcceptReportInstanceContent.cs` - Report content acceptance
- `ChartSpec.cs` - Chart specification
- `HTMLReportEngine.cs` - HTML report engine
- `ReportEntityLoaderModule.cs` - Report entity loader
- `ReportGroomingService.cs` - Report grooming
- `ReportInstance.cs` - Report instance model
- `ReportInstanceReferenceBuilder.cs` - Report reference builder
- `ReportInstanceViewModel.cs` - Report view model
- `ReportLogic.cs` - Report business logic
- `ReportRepository.cs` - Report storage
- `ReportTemplate.cs` - Report template model
- `ReportTemplateContentDomainSource.cs` - Report content source
- `ReportTemplateViewModel.cs` - Report template view model

**RuleActions**:
- `RuleActionCreateReport.cs` - Create report action
- `RuleActionReportEnrollDashboard.cs` - Enroll report in dashboard

#### 3.15 Rules (`/Platform/Rules/`)
**Purpose**: Rule engine and automation system

**Files**:
- `IRuleActionEvaluator.cs` - Rule action evaluator interface
- `Rule.cs` - Rule model
- `RuleAction.cs` - Rule action base
- `RuleActionNoOp.cs` - No-operation action
- `RuleCondition.cs` - Rule condition model
- `RuleContentDomainSource.cs` - Rule content source
- `RuleEngine.cs` - Rule engine implementation
- `RuleEvaluator.cs` - Rule evaluation logic
- `RuleEvaluatorOptions.cs` - Evaluator options
- `RuleExpressionInvocation.cs` - Expression invocation
- `RuleServiceCollectionExtensions.cs` - DI extensions
- `RuleUtil.cs` - Rule utilities

**EventAppenders**:
- `AddEntityReferenceAppender.cs` - Add entity reference
- `AddFreeJsonAppender.cs` - Add free JSON
- `ComputedDateTimeAppender.cs` - Compute date/time
- `CurrentDateTimeAppender.cs` - Current date/time
- `EventAppenderUtility.cs` - Appender utilities
- `FindReplaceAppender.cs` - Find and replace
- `IEventAppender.cs` - Appender interface
- `JsonWinnowerAppender.cs` - JSON winnowing
- `LoadEntityDataFromReferenceAppender.cs` - Load entity data
- `SelectStoreAppender.cs` - Select and store

**RuleActions**:
- `RuleActionCustomEvent.cs` - Custom event action
- `RuleActionEntityEnrollDashboardBase.cs` - Base class for entity dashboard enrollment
- `RuleActionForEach.cs` - For each loop action
- `RuleActionLogEventData.cs` - Log event data action

#### 3.16 Scheduler (`/Platform/Scheduler/`)
**Purpose**: Job scheduling system

**Files**:
- `AcceptScheduledEventContentInstance.cs` - Scheduled event content
- `AttachedScheduleReference.cs` - Schedule reference
- `CronExpression.cs` - Cron expression parser
- `IJobIntegration.cs` - Job integration interface
- `Schedule.cs` - Schedule model
- `ScheduledEvent.cs` - Scheduled event model
- `ScheduledEventContentDomainSource.cs` - Event content source
- `ScheduledEventIdentifier.cs` - Event identifier
- `ScheduledEventTemplate.cs` - Event template
- `ScheduledJobEntity.cs` - Job entity model
- `SchedulerBackgroundWorker.cs` - Background worker
- `SchedulerLogic.cs` - Scheduler logic
- `SchedulerRepository.cs` - Scheduler storage
- `SinkEventJob.cs` - Sink event job

**RuleActions**:
(No RuleActions found in Scheduler directory)

#### 3.17 Tables (`/Platform/Tables/`)
**Purpose**: Dynamic table system

**Files**:
- `AcceptTableViewContent.cs` - Table view content
- `ColDef.cs` - Column definition
- `KeyType.cs` - Key type enumeration
- `Mapping.cs` - Data mapping
- `NumericBin.cs` - Numeric binning
- `ProjectionMapper.cs` - Projection mapping
- `QueryOrdering.cs` - Query ordering
- `RegisteredTableQueryContentDomainSource.cs` - Query content source
- `RegisteredTableQueryTemplate.cs` - Query template
- `RegisteredTableQueryWorkItemAssociation.cs` - Query work item association
- `RegisteredTableQueryWorkItemAssociationRepository.cs` - Association storage
- `RegisteredTableSummarizationContentDomainSource.cs` - Summarization source
- `RegisteredTableSummarizationTemplate.cs` - Summarization template
- `RelativeTableQueryCommand.cs` - Relative query command
- `SummaryComputation.cs` - Summary computation
- `TableDataRepository.cs` - Table data storage
- `TableEntityLoaderModule.cs` - Table entity loader
- `TableEntityReferenceBuilder.cs` - Table reference builder
- `TableGroomingService.cs` - Table grooming
- `TableLogic.cs` - Table business logic
- `TableMetadata.cs` - Table metadata model
- `TableMetadataRepository.cs` - Metadata storage
- `TableQueryCommand.cs` - Query command
- `TableRowConverter.cs` - Row converter
- `TableRowData.cs` - Row data model
- `TableSummarizationCommand.cs` - Summarization command
- `TableSummaryRow.cs` - Summary row model
- `TableSummaryViewModel.cs` - Summary view model
- `TableTemplate.cs` - Table template
- `TableTemplateContentDomainSource.cs` - Table content source
- `TableViewModel.cs` - Table view model

**RuleActions**:
- `RuleActionDeleteTableRow.cs` - Delete table row
- `RuleActionEditTableRow.cs` - Edit table row
- `RuleActionInsertTableData.cs` - Insert table data
- `RuleActionSelectTableRows.cs` - Select table rows
- `RuleActionSummarizeTable.cs` - Summarize table
- `RuleActionTableQueryEnrollDashboard.cs` - Enroll table query in dashboard

#### 3.18 Tags (`/Platform/Tags/`)
**Purpose**: Tagging system

**Files**:
- `ITaggable.cs` - Taggable interface
- `TagCountsDataModel.cs` - Tag counts model
- `TagCountsRepository.cs` - Tag counts storage
- `TagUtil.cs` - Tag utilities
- `Tagger.cs` - Tagging implementation

#### 3.19 Terminology (`/Platform/Terminology/`)
**Purpose**: Application terminology management

**Files**:
- `ApplicationTermsFile.cs` - File-based terms
- `DefaultTerminology.cs` - Default terms
- `FileApplicationTermsOptions.cs` - File terms options
- `IApplicationTerms.cs` - Terms interface
- `TermKey.cs` - Term key enumeration

#### 3.20 WorkItems (`/Platform/WorkItems/`)
**Purpose**: Work item tracking system

**Files**:
- `AcceptWorkItemInstanceContent.cs` - Work item content
- `CreateWorkItemCommand.cs` - Create work item command
- `PriorityTemplate.cs` - Priority template
- `Section.cs` - Work item section
- `SectionTemplate.cs` - Section template
- `SectionViewModel.cs` - Section view model
- `StatusTemplate.cs` - Status template
- `StatusType.cs` - Status type enumeration
- `TriageTemplate.cs` - Triage template
- `WorkItem.cs` - Work item model
- `WorkItemBookmark.cs` - Bookmark model
- `WorkItemBookmarkViewModel.cs` - Bookmark view model
- `WorkItemEventHistory.cs` - Event history
- `WorkItemEventHistoryViewModel.cs` - History view model
- `WorkItemGroomBehavior.cs` - Groom behavior
- `WorkItemGroomingService.cs` - Grooming service
- `WorkItemLink.cs` - Work item link
- `WorkItemLoaderModule.cs` - Work item loader
- `WorkItemLogic.cs` - Work item logic
- `WorkItemReferenceBuilder.cs` - Reference builder
- `WorkItemRepository.cs` - Work item storage
- `WorkItemSummaryViewModel.cs` - Summary view model
- `WorkItemTemplate.cs` - Work item template
- `WorkItemTemplateDomainSource.cs` - Template source
- `WorkItemTemplateViewModel.cs` - Template view model
- `WorkItemViewModel.cs` - Work item view model

**RuleActions**:
- `RuleActionAddWorkItemBookmark.cs` - Add work item bookmark
- `RuleActionAddWorkItemLink.cs` - Add work item link
- `RuleActionCreateWorkItem.cs` - Create work item
- `RuleActionEditWorkItemMetadata.cs` - Edit work item metadata
- `RuleActionRemoveWorkItemBookmark.cs` - Remove work item bookmark
- `RuleActionRemoveWorkItemLink.cs` - Remove work item link
- `RuleActionWorkItemAddSectionEntity.cs` - Add section entity
- `RuleActionWorkItemEnrollDashboard.cs` - Enroll work item in dashboard
- `RuleActionWorkItemRemoveSectionEntity.cs` - Remove section entity

#### 3.21 WorkSets (`/Platform/WorkSets/`)
**Purpose**: Work set (dashboard) system

**Files**:
- `AcceptWorkSetInstanceContent.cs` - Work set content
- `BuildDashboardJob.cs` - Dashboard build job
- `CreatableWorkItem.cs` - Creatable work item
- `CreateWorkSetInstanceCommand.cs` - Create work set command
- `DashboardCandidate.cs` - Dashboard candidate
- `DashboardCandidateRepository.cs` - Candidate storage
- `DashboardItemViewModel.cs` - Dashboard item view
- `GridColumnSizes.cs` - Grid column sizes
- `InitialViewData.cs` - Initial view data
- `ViewColumnDef.cs` - View column definition
- `ViewDataQuery.cs` - View data query
- `ViewRowDef.cs` - View row definition
- `WorkSet.cs` - Work set model
- `WorkSetHome.cs` - Work set home
- `WorkSetInteractivityState.cs` - Interactivity state
- `WorkSetLoaderModule.cs` - Work set loader
- `WorkSetLogic.cs` - Work set logic
- `WorkSetManagement.cs` - Work set management
- `WorkSetMember.cs` - Work set member
- `WorkSetMemberRepository.cs` - Member storage
- `WorkSetMemberViewModel.cs` - Member view model
- `WorkSetMenuItem.cs` - Menu item model
- `WorkSetReferenceBuilder.cs` - Reference builder
- `WorkSetRepository.cs` - Work set storage
- `WorkSetSummaryViewModel.cs` - Summary view model
- `WorkSetTemplate.cs` - Work set template
- `WorkSetTemplateDomainSource.cs` - Template source
- `WorkSetTemplateViewModel.cs` - Template view model
- `WorkSetViewModel.cs` - Work set view model

**EventAppenders**:
- `LoadWorkSetAppender.cs` - Load work set appender

**RuleActions**:
- `RuleActionCreateWorkSet.cs` - Create work set
- `RuleActionDeleteWorkSet.cs` - Delete work set
- `RuleActionSetWorkSetMetadata.cs` - Set work set metadata
- `RuleActionWorkSetAddMember.cs` - Add work set member
- `RuleActionWorkSetRemoveMember.cs` - Remove work set member

**Additional Platform Files**:
- `BFormOptions.cs` - Platform configuration options
- `WorkSetAndItemFinder.cs` - Work set and item finder utility

### 4. Repository (`/Repository/`)
**Purpose**: Data access layer abstractions

#### 4.1 Interfaces (`/Repository/Interfaces/`)
**Purpose**: Repository pattern interfaces

**Files**:
- `IDataEnvironment.cs` - Data environment interface
- `IDataModel.cs` - Data model interface
- `IRepository.cs` - Repository interface
- `ITransactionContext.cs` - Transaction context interface
- `RepositoryContext.cs` - Repository context

#### 4.2 Mongo (`/Repository/Mongo/`)
**Purpose**: MongoDB implementation

**Files**:
- `MongoDataEnvironment.cs` - MongoDB environment
- `MongoDatabaseExtensions.cs` - MongoDB extensions
- `MongoEnvironment.cs` - MongoDB configuration
- `MongoRepository.cs` - MongoDB repository implementation
- `MongoRepositoryOptions.cs` - MongoDB options
- `MongoTransactionContext.cs` - MongoDB transactions

### 5. SimilarEntityTracking (`/SimilarEntityTracking/`)
**Purpose**: Duplicate detection and consolidation

#### 5.1 ConsolidateDigest (`/SimilarEntityTracking/ConsolidateDigest/`)
**Purpose**: Digest consolidation system

**Files**:
- `ConsolidateDigestMessage.cs` - Consolidate message
- `ConsolidateToDigestOrder.cs` - Consolidation order
- `ConsolidatedDigest.cs` - Consolidated digest model
- `ConsolidatedDigestRepository.cs` - Digest storage
- `ConsolidationCore.cs` - Core consolidation logic
- `ConsolidationService.cs` - Consolidation service
- `DigestDistributionOptions.cs` - Distribution options
- `DigestDistributionService.cs` - Distribution service
- `DigestEntryModel.cs` - Digest entry model
- `DigestItem.cs` - Digest item
- `DigestModel.cs` - Digest model
- `DigestReadyEventArgs.cs` - Digest ready event
- `DigestResultReceiver.cs` - Result receiver
- `Digestible.cs` - Digestible base class
- `IConsolidatedDigest.cs` - Digest interface
- `IConsolidationCore.cs` - Core interface
- `IDigestible.cs` - Digestible interface

#### 5.2 DuplicateSuppression (`/SimilarEntityTracking/DuplicateSuppression/`)
**Purpose**: Duplicate suppression system

**Files**:
- `DuplicateSuppressionCore.cs` - Core suppression logic
- `DuplicateSuppressionService.cs` - Suppression service
- `ICanShutUp.cs` - Can be suppressed interface
- `ISuppressionPersistence.cs` - Suppression persistence
- `IWillShutUp.cs` - Will be suppressed interface
- `ItemAllowedEventArgs.cs` - Item allowed event
- `ItemSuppressedEventArgs.cs` - Item suppressed event
- `RepoSuppressionPersistence.cs` - Repository persistence
- `SuppressDuplicatesMessage.cs` - Suppress message
- `SuppressedItem.cs` - Suppressed item model
- `Suppressible.cs` - Suppressible base class
- `SuppressionGroomingService.cs` - Grooming service
- `SuppressionOrder.cs` - Suppression order
- `SuppressionRepository.cs` - Suppression storage
- `SuppressionResultReceiver.cs` - Result receiver

**Additional Files**:
- `ITrackSimilar.cs` - Similar tracking interface

### 6. Utility (`/Utility/`)
**Purpose**: Common utilities and helpers

**Files**:
- `AnonymousDisposable.cs` - Anonymous disposable pattern
- `AsyncHelper.cs` - Async helper methods
- `Base32Encoder.cs` - Base32 encoding
- `BetterBsonSerializer.cs` - Enhanced BSON serialization
- `BsonDocumentExtensions.cs` - BSON extensions
- `ControllerUserAuthorizationExtensions.cs` - Auth extensions
- `DateTimeUtility.cs` - DateTime utilities
- `Disposable.cs` - Disposable base class
- `EmptyifNull.cs` - Null handling utility
- `EnumExtensions.cs` - Enum extensions
- `EnumerableEx.cs` - Enumerable extensions
- `ExceptionExtensions.cs` - Exception extensions
- `ExpressionEvaluator.cs` - Expression evaluation
- `GuidEncoder.cs` - GUID encoding
- `Hash.cs` - Hashing utilities
- `JTokenTypeConverter.cs` - JToken type converter
- `JsonUtility.cs` - JSON utilities
- `JsonWinnower.cs` - JSON winnowing
- `KeyInject.cs` - Key injection
- `KeyTypeConverter.cs` - Key type converter
- `LinqPartitionExtensions.cs` - LINQ partitioning
- `Multimap.cs` - Multi-map collection
- `OfOne.cs` - Single item collection
- `OrderingTypeConverter.cs` - Ordering converter
- `RandomExtensions.cs` - Random extensions
- `Retry.cs` - Retry logic
- `RunOnce.cs` - Run once pattern
- `ScopedServiceResolver.cs` - Service resolver
- `SelectMany.cs` - SelectMany utilities
- `TemporalCollocator.cs` - Temporal collocation
- `TemporalDivisions.cs` - Temporal divisions
- `TimeFrame.cs` - Time frame model
- `TypeNameExtensions.cs` - Type name extensions
- `UriExtensions.cs` - URI extensions

#### 6.1 Caching (`/Utility/Caching/`)
**Purpose**: Caching utilities

**Files**:
- `ICachedData.cs` - Cached data interface
- `InMemoryCachedData.cs` - In-memory cache

#### 6.2 CompletionTracking (`/Utility/CompletionTracking/`)
**Purpose**: Work completion tracking

**Files**:
- `ITrackWorking.cs` - Work tracking interface
- `InMemoryTrackWorking.cs` - In-memory tracking
- `RedisTrackWorking.cs` - Redis-based tracking

### 7. Validation (`/Validation/`)
**Purpose**: Data validation framework

**Files**:
- `DoesNotHaveLengthException.cs` - Length validation exception
- `GuaranteesValidator.cs` - Guarantees validator
- `HasLengthException.cs` - Has length exception
- `IValidator.cs` - Validator interface
- `IsLongerOrEqualException.cs` - Length comparison exception
- `IsLongerThanException.cs` - Length comparison exception
- `IsNotLongerOrEqualException.cs` - Length comparison exception
- `IsNotLongerThanException.cs` - Length comparison exception
- `IsNotNullException.cs` - Not null exception
- `IsNotOfTypeException.cs` - Type validation exception
- `IsNotShorterException.cs` - Length comparison exception
- `IsNotShorterOrEqualException.cs` - Length comparison exception
- `IsNullException.cs` - Null exception
- `IsOfTypeException.cs` - Type validation exception
- `IsShorterException.cs` - Length comparison exception
- `IsShorterOrEqualException.cs` - Length comparison exception
- `ObjectValidator.cs` - Object validator
- `OtherwiseValidator.cs` - Otherwise validator
- `RequiresAttribute.cs` - Requires attribute
- `RequiresValidator.cs` - Requires validator
- `ValidationFormat.cs` - Validation format
- `ValidatorBase.cs` - Validator base class
- `ValidatorExtensions.cs` - Validator extensions

## Summary

The BFormDomain project is a comprehensive business forms and workflow platform with the following major components:

1. **Core Infrastructure**: Diagnostics, Message Bus, Repository patterns
2. **Platform Features**: 
   - Event-driven architecture (AppEvents)
   - Dynamic forms and entities
   - KPI tracking and reporting
   - Work item and work set management
   - Rule engine for automation
   - Notification system
   - Scheduling and job management
3. **Supporting Systems**: 
   - Authentication/Authorization
   - Content management
   - File management
   - Tagging system
   - Duplicate detection
4. **Utilities**: Comprehensive utility library for common operations
5. **Validation**: Fluent validation framework

The architecture follows Domain-Driven Design principles with clear separation of concerns, repository patterns, and extensive use of interfaces for flexibility and testability.