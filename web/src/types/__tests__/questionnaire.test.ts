/**
 * questionnaire.ts Helper Function Tests
 *
 * Tests type helper functions for questionnaire management.
 * Coverage Target: 80%+
 */

import {
  getQuestionnaireTypeLabel,
  getQuestionTypeLabel,
  getResponseStatusLabel,
  isQuestionTypeWithOptions,
  isQuestionTypeWithMultipleValues,
  getQuestionValidationRules,
  QuestionnaireType,
  QuestionType,
  ResponseStatus,
  QuestionnaireQuestion,
  QUESTIONNAIRE_TYPE_LABELS,
  QUESTION_TYPE_LABELS,
  RESPONSE_STATUS_LABELS,
} from '../questionnaire';

describe('questionnaire.ts - Helper Functions', () => {
  describe('getQuestionnaireTypeLabel', () => {
    it('returns correct label for General type', () => {
      expect(getQuestionnaireTypeLabel(QuestionnaireType.General)).toBe('General');
    });

    it('returns correct label for ProjectIntake type', () => {
      expect(getQuestionnaireTypeLabel(QuestionnaireType.ProjectIntake)).toBe('Project Intake');
    });

    it('returns correct label for ClientOnboarding type', () => {
      expect(getQuestionnaireTypeLabel(QuestionnaireType.ClientOnboarding)).toBe('Client Onboarding');
    });

    it('returns correct label for ProviderVetting type', () => {
      expect(getQuestionnaireTypeLabel(QuestionnaireType.ProviderVetting)).toBe('Provider Vetting');
    });

    it('returns correct label for ProjectFeedback type', () => {
      expect(getQuestionnaireTypeLabel(QuestionnaireType.ProjectFeedback)).toBe('Project Feedback');
    });

    it('returns correct label for SkillAssessment type', () => {
      expect(getQuestionnaireTypeLabel(QuestionnaireType.SkillAssessment)).toBe('Skill Assessment');
    });

    it('returns correct label for MarketResearch type', () => {
      expect(getQuestionnaireTypeLabel(QuestionnaireType.MarketResearch)).toBe('Market Research');
    });

    it('returns Unknown for invalid type', () => {
      expect(getQuestionnaireTypeLabel('invalid' as unknown as QuestionnaireType)).toBe('Unknown');
      expect(getQuestionnaireTypeLabel(999 as unknown as QuestionnaireType)).toBe('Unknown');
    });
  });

  describe('getQuestionTypeLabel', () => {
    it('returns correct label for Text type', () => {
      expect(getQuestionTypeLabel(QuestionType.Text)).toBe('Short Text');
    });

    it('returns correct label for LongText type', () => {
      expect(getQuestionTypeLabel(QuestionType.LongText)).toBe('Long Text');
    });

    it('returns correct label for Number type', () => {
      expect(getQuestionTypeLabel(QuestionType.Number)).toBe('Number');
    });

    it('returns correct label for Email type', () => {
      expect(getQuestionTypeLabel(QuestionType.Email)).toBe('Email');
    });

    it('returns correct label for Phone type', () => {
      expect(getQuestionTypeLabel(QuestionType.Phone)).toBe('Phone');
    });

    it('returns correct label for Date type', () => {
      expect(getQuestionTypeLabel(QuestionType.Date)).toBe('Date');
    });

    it('returns correct label for Time type', () => {
      expect(getQuestionTypeLabel(QuestionType.Time)).toBe('Time');
    });

    it('returns correct label for DateTime type', () => {
      expect(getQuestionTypeLabel(QuestionType.DateTime)).toBe('Date & Time');
    });

    it('returns correct label for Boolean type', () => {
      expect(getQuestionTypeLabel(QuestionType.Boolean)).toBe('Yes/No');
    });

    it('returns correct label for Radio type', () => {
      expect(getQuestionTypeLabel(QuestionType.Radio)).toBe('Radio Button');
    });

    it('returns correct label for Checkbox type', () => {
      expect(getQuestionTypeLabel(QuestionType.Checkbox)).toBe('Checkbox');
    });

    it('returns correct label for Dropdown type', () => {
      expect(getQuestionTypeLabel(QuestionType.Dropdown)).toBe('Dropdown');
    });

    it('returns correct label for MultipleChoice type', () => {
      expect(getQuestionTypeLabel(QuestionType.MultipleChoice)).toBe('Multiple Choice');
    });

    it('returns correct label for Rating type', () => {
      expect(getQuestionTypeLabel(QuestionType.Rating)).toBe('Rating');
    });

    it('returns correct label for FileUpload type', () => {
      expect(getQuestionTypeLabel(QuestionType.FileUpload)).toBe('File Upload');
    });

    it('returns correct label for Url type', () => {
      expect(getQuestionTypeLabel(QuestionType.Url)).toBe('URL');
    });

    it('returns Unknown for invalid type', () => {
      expect(getQuestionTypeLabel('invalid' as unknown as QuestionType)).toBe('Unknown');
      expect(getQuestionTypeLabel(999 as unknown as QuestionType)).toBe('Unknown');
    });
  });

  describe('getResponseStatusLabel', () => {
    it('returns correct label for Draft status', () => {
      expect(getResponseStatusLabel(ResponseStatus.Draft)).toBe('Draft');
    });

    it('returns correct label for Submitted status', () => {
      expect(getResponseStatusLabel(ResponseStatus.Submitted)).toBe('Submitted');
    });

    it('returns correct label for UnderReview status', () => {
      expect(getResponseStatusLabel(ResponseStatus.UnderReview)).toBe('Under Review');
    });

    it('returns correct label for Approved status', () => {
      expect(getResponseStatusLabel(ResponseStatus.Approved)).toBe('Approved');
    });

    it('returns correct label for Rejected status', () => {
      expect(getResponseStatusLabel(ResponseStatus.Rejected)).toBe('Rejected');
    });

    it('returns correct label for NeedsRevision status', () => {
      expect(getResponseStatusLabel(ResponseStatus.NeedsRevision)).toBe('Needs Revision');
    });

    it('returns Unknown for invalid status', () => {
      expect(getResponseStatusLabel('invalid' as unknown as ResponseStatus)).toBe('Unknown');
      expect(getResponseStatusLabel(999 as unknown as ResponseStatus)).toBe('Unknown');
    });
  });

  describe('isQuestionTypeWithOptions', () => {
    it('returns true for Radio type', () => {
      expect(isQuestionTypeWithOptions(QuestionType.Radio)).toBe(true);
    });

    it('returns true for Checkbox type', () => {
      expect(isQuestionTypeWithOptions(QuestionType.Checkbox)).toBe(true);
    });

    it('returns true for Dropdown type', () => {
      expect(isQuestionTypeWithOptions(QuestionType.Dropdown)).toBe(true);
    });

    it('returns true for MultipleChoice type', () => {
      expect(isQuestionTypeWithOptions(QuestionType.MultipleChoice)).toBe(true);
    });

    it('returns false for Text type', () => {
      expect(isQuestionTypeWithOptions(QuestionType.Text)).toBe(false);
    });

    it('returns false for LongText type', () => {
      expect(isQuestionTypeWithOptions(QuestionType.LongText)).toBe(false);
    });

    it('returns false for Number type', () => {
      expect(isQuestionTypeWithOptions(QuestionType.Number)).toBe(false);
    });

    it('returns false for Date type', () => {
      expect(isQuestionTypeWithOptions(QuestionType.Date)).toBe(false);
    });

    it('returns false for Email type', () => {
      expect(isQuestionTypeWithOptions(QuestionType.Email)).toBe(false);
    });

    it('returns false for Boolean type', () => {
      expect(isQuestionTypeWithOptions(QuestionType.Boolean)).toBe(false);
    });

    it('returns false for Rating type', () => {
      expect(isQuestionTypeWithOptions(QuestionType.Rating)).toBe(false);
    });

    it('returns false for FileUpload type', () => {
      expect(isQuestionTypeWithOptions(QuestionType.FileUpload)).toBe(false);
    });
  });

  describe('isQuestionTypeWithMultipleValues', () => {
    it('returns true for Checkbox type', () => {
      expect(isQuestionTypeWithMultipleValues(QuestionType.Checkbox)).toBe(true);
    });

    it('returns false for Radio type', () => {
      expect(isQuestionTypeWithMultipleValues(QuestionType.Radio)).toBe(false);
    });

    it('returns false for Text type', () => {
      expect(isQuestionTypeWithMultipleValues(QuestionType.Text)).toBe(false);
    });

    it('returns false for Dropdown type', () => {
      expect(isQuestionTypeWithMultipleValues(QuestionType.Dropdown)).toBe(false);
    });

    it('returns false for MultipleChoice type', () => {
      expect(isQuestionTypeWithMultipleValues(QuestionType.MultipleChoice)).toBe(false);
    });

    it('returns false for Number type', () => {
      expect(isQuestionTypeWithMultipleValues(QuestionType.Number)).toBe(false);
    });
  });

  describe('getQuestionValidationRules', () => {
    const createMockQuestion = (overrides: Partial<QuestionnaireQuestion> = {}): QuestionnaireQuestion => ({
      id: '1',
      questionnaireId: 'q1',
      questionText: 'Test question',
      type: QuestionType.Text,
      isRequired: false,
      displayOrder: 1,
      isActive: true,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      options: [],
      ...overrides,
    });

    it('returns empty rules for optional question without constraints', () => {
      const question = createMockQuestion({
        questionText: 'Optional question',
        isRequired: false,
      });

      const rules = getQuestionValidationRules(question);
      expect(rules).toEqual({});
    });

    it('returns required rule for required question', () => {
      const question = createMockQuestion({
        questionText: 'Required question',
        isRequired: true,
      });

      const rules = getQuestionValidationRules(question);
      expect(rules.required).toBe('This field is required');
    });

    it('returns minLength rule for text question with minValue', () => {
      const question = createMockQuestion({
        questionText: 'Text with min',
        type: QuestionType.Text,
        minValue: 10,
      });

      const rules = getQuestionValidationRules(question);
      expect(rules.minLength).toEqual({
        value: 10,
        message: 'Minimum length is 10 characters',
      });
    });

    it('returns maxLength rule for text question with maxValue', () => {
      const question = createMockQuestion({
        questionText: 'Text with max',
        type: QuestionType.Text,
        maxValue: 100,
      });

      const rules = getQuestionValidationRules(question);
      expect(rules.maxLength).toEqual({
        value: 100,
        message: 'Maximum length is 100 characters',
      });
    });

    it('does not add minLength for non-text questions with minValue', () => {
      const question = createMockQuestion({
        questionText: 'Number question',
        type: QuestionType.Number,
        minValue: 10,
      });

      const rules = getQuestionValidationRules(question);
      expect(rules.minLength).toBeUndefined();
    });

    it('does not add maxLength for non-text questions with maxValue', () => {
      const question = createMockQuestion({
        questionText: 'Number question',
        type: QuestionType.Number,
        maxValue: 100,
      });

      const rules = getQuestionValidationRules(question);
      expect(rules.maxLength).toBeUndefined();
    });

    it('returns pattern rule for question with validationRegex', () => {
      const question = createMockQuestion({
        questionText: 'Email question',
        validationRegex: '^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$',
        validationMessage: 'Please enter a valid email',
      });

      const rules = getQuestionValidationRules(question);
      expect(rules.pattern).toBeDefined();
      expect(rules.pattern.value).toBeInstanceOf(RegExp);
      expect(rules.pattern.message).toBe('Please enter a valid email');
    });

    it('uses default message when validationMessage not provided', () => {
      const question = createMockQuestion({
        questionText: 'Regex question',
        validationRegex: '^[A-Z]+$',
      });

      const rules = getQuestionValidationRules(question);
      expect(rules.pattern).toBeDefined();
      expect(rules.pattern.message).toBe('Invalid format');
    });

    it('combines multiple rules for complex question', () => {
      const question = createMockQuestion({
        questionText: 'Complex question',
        type: QuestionType.Text,
        isRequired: true,
        minValue: 5,
        maxValue: 50,
        validationRegex: '^[a-z]+$',
        validationMessage: 'Only lowercase letters',
      });

      const rules = getQuestionValidationRules(question);
      expect(rules.required).toBe('This field is required');
      expect(rules.minLength.value).toBe(5);
      expect(rules.maxLength.value).toBe(50);
      expect(rules.pattern.message).toBe('Only lowercase letters');
    });

    it('handles question with minValue of 0', () => {
      const question = createMockQuestion({
        type: QuestionType.Text,
        minValue: 0,
      });

      const rules = getQuestionValidationRules(question);
      // minValue of 0 should still create a rule (0 !== undefined)
      expect(rules.minLength).toEqual({
        value: 0,
        message: 'Minimum length is 0 characters',
      });
    });

    it('handles question with maxValue of 0', () => {
      const question = createMockQuestion({
        type: QuestionType.Text,
        maxValue: 0,
      });

      const rules = getQuestionValidationRules(question);
      // maxValue of 0 should still create a rule (0 !== undefined)
      expect(rules.maxLength).toEqual({
        value: 0,
        message: 'Maximum length is 0 characters',
      });
    });
  });

  describe('Constants - Label Records', () => {
    it('QUESTIONNAIRE_TYPE_LABELS covers all QuestionnaireType values', () => {
      const allTypes = [
        QuestionnaireType.General,
        QuestionnaireType.ProjectIntake,
        QuestionnaireType.ClientOnboarding,
        QuestionnaireType.ProviderVetting,
        QuestionnaireType.ProjectFeedback,
        QuestionnaireType.SkillAssessment,
        QuestionnaireType.MarketResearch,
      ];

      allTypes.forEach((type) => {
        expect(QUESTIONNAIRE_TYPE_LABELS[type]).toBeDefined();
        expect(typeof QUESTIONNAIRE_TYPE_LABELS[type]).toBe('string');
      });
    });

    it('QUESTION_TYPE_LABELS covers all QuestionType values', () => {
      const allTypes = [
        QuestionType.Text,
        QuestionType.LongText,
        QuestionType.Number,
        QuestionType.Email,
        QuestionType.Phone,
        QuestionType.Date,
        QuestionType.Time,
        QuestionType.DateTime,
        QuestionType.Boolean,
        QuestionType.Radio,
        QuestionType.Checkbox,
        QuestionType.Dropdown,
        QuestionType.MultipleChoice,
        QuestionType.Rating,
        QuestionType.FileUpload,
        QuestionType.Url,
      ];

      allTypes.forEach((type) => {
        expect(QUESTION_TYPE_LABELS[type]).toBeDefined();
        expect(typeof QUESTION_TYPE_LABELS[type]).toBe('string');
      });
    });

    it('RESPONSE_STATUS_LABELS covers all ResponseStatus values', () => {
      const allStatuses = [
        ResponseStatus.Draft,
        ResponseStatus.Submitted,
        ResponseStatus.UnderReview,
        ResponseStatus.Approved,
        ResponseStatus.Rejected,
        ResponseStatus.NeedsRevision,
      ];

      allStatuses.forEach((status) => {
        expect(RESPONSE_STATUS_LABELS[status]).toBeDefined();
        expect(typeof RESPONSE_STATUS_LABELS[status]).toBe('string');
      });
    });
  });
});
