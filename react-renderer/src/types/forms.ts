/**
 * Form related types and interfaces
 */

import { IAppEntity, SatelliteJson } from './common';

export interface FormInstance extends IAppEntity {
  entityType: 'FormInstance';
  home: FormInstanceHome;
  content?: Record<string, any>;
  jsonContent?: Record<string, any>;
}

export enum FormInstanceHome {
  Standalone = 'Standalone',
  WorkSet = 'WorkSet',
  WorkItem = 'WorkItem',
  Section = 'Section'
}

export interface FormTemplate {
  domainName: string;
  name: string;
  comments?: string;
  title: string;
  descendingOrder: number;
  submitTitle?: string;
  contentSchemaNeedsReplacements: boolean;
  isVisibleToUsers: boolean;
  eventsOnly: boolean;
  revertToDefaultsOnSubmit: boolean;
  tags: string[];
  satelliteData: Record<string, string>;
  contentSchema: SatelliteJson;
  uiSchema: SatelliteJson;
  yupSchema: SatelliteJson;
  iconClass?: string;
  actionButtons: ActionButton[];
}

export interface ActionButton {
  id: string;
  title: string;
  iconClass?: string;
  buttonClass?: string;
  action: ActionButtonAction;
  isSubmit: boolean;
  confirmationMessage?: string;
}

export enum ActionButtonAction {
  Submit = 'Submit',
  Custom = 'Custom',
  Cancel = 'Cancel',
  Reset = 'Reset'
}

export interface FormInstanceViewModel {
  id: string;
  title: string;
  templateName: string;
  home: FormInstanceHome;
  tags: string[];
  createdDate: string;
  updatedDate: string;
  content: Record<string, any>;
  schema: JSONSchema7;
  uiSchema: UiSchema;
  validationSchema: any; // Yup schema
  actionButtons: ActionButton[];
  isReadOnly: boolean;
  showValidation: boolean;
}

export interface FormTemplateViewModel {
  name: string;
  title: string;
  description?: string;
  tags: string[];
  isVisibleToUsers: boolean;
  schema: JSONSchema7;
  uiSchema: UiSchema;
  validationSchema: any; // Yup schema
  actionButtons: ActionButton[];
  submitTitle?: string;
  iconClass?: string;
}

// JSON Schema types (subset of JSONSchema7)
export interface JSONSchema7 {
  $schema?: string;
  $id?: string;
  title?: string;
  description?: string;
  type?: JSONSchema7TypeName | JSONSchema7TypeName[];
  properties?: Record<string, JSONSchema7>;
  required?: string[];
  additionalProperties?: boolean | JSONSchema7;
  items?: JSONSchema7 | JSONSchema7[];
  enum?: any[];
  const?: any;
  format?: string;
  pattern?: string;
  minimum?: number;
  maximum?: number;
  minLength?: number;
  maxLength?: number;
  minItems?: number;
  maxItems?: number;
  default?: any;
  examples?: any[];
  definitions?: Record<string, JSONSchema7>;
  $ref?: string;
  if?: JSONSchema7;
  then?: JSONSchema7;
  else?: JSONSchema7;
  allOf?: JSONSchema7[];
  anyOf?: JSONSchema7[];
  oneOf?: JSONSchema7[];
  not?: JSONSchema7;
}

export type JSONSchema7TypeName = 
  | 'string'
  | 'number'
  | 'integer'
  | 'object'
  | 'array'
  | 'boolean'
  | 'null';

// UI Schema types for react-jsonschema-form
export interface UiSchema {
  'ui:widget'?: string;
  'ui:field'?: string;
  'ui:placeholder'?: string;
  'ui:title'?: string;
  'ui:description'?: string;
  'ui:help'?: string;
  'ui:order'?: string[];
  'ui:options'?: Record<string, any>;
  'ui:disabled'?: boolean;
  'ui:readonly'?: boolean;
  'ui:hidden'?: boolean;
  'ui:autofocus'?: boolean;
  'ui:emptyValue'?: any;
  'ui:enumDisabled'?: any[];
  'ui:classNames'?: string;
  'ui:style'?: React.CSSProperties;
  [key: string]: any;
}

export interface FormValidationError {
  property: string;
  message: string;
  value?: any;
  schema?: JSONSchema7;
}

export interface FormSubmitEvent {
  formData: any;
  formId: string;
  action: ActionButtonAction;
  errors?: FormValidationError[];
}