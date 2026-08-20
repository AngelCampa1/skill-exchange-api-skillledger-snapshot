'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect, useRef, useCallback } from 'react';
import { Search, X, AlertCircle } from 'lucide-react';
import { AUTH_CONFIG } from '../constants/auth';

interface Skill {
  id: string;
  name: string;
  description?: string;
  category: string;
  isSystemManaged: boolean;
  isActive: boolean;
  createdAt: string;
}

interface SkillCategory {
  name: string;
  skillCount: number;
  userCount: number;
}

export interface SelectedSkill {
  skillId: string;
  skillName: string;
  category: string;
  proficiency: 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert';
  yearsOfExperience?: number;
  notes?: string;
}

interface SkillSelectorProps {
  selectedSkills: SelectedSkill[];
  onSkillsChange: (skills: SelectedSkill[]) => void;
  minSkills?: number;
  maxSkills?: number;
}

const proficiencyLevels = ['Beginner', 'Intermediate', 'Advanced', 'Expert'] as const;
const proficiencyColors = {
  Beginner: 'bg-muted text-muted-foreground',
  Intermediate: 'bg-primary/10 text-primary',
  Advanced: 'bg-success/10 text-success',
  Expert: 'bg-accent/10 text-accent'
};

const proficiencyDescriptions = {
  Beginner: 'Learning the fundamentals',
  Intermediate: 'Can work with guidance',
  Advanced: 'Can work independently',
  Expert: 'Can teach and mentor others'
};

export default function SkillSelector({
  selectedSkills,
  onSkillsChange,
  minSkills = 0,
  maxSkills = 50
}: SkillSelectorProps) {
  const [availableSkills, setAvailableSkills] = useState<Skill[]>([]);
  const [categories, setCategories] = useState<SkillCategory[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showDropdown, setShowDropdown] = useState(false);
  const [isSearching, setIsSearching] = useState(false);
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  
  // P1 PERFORMANCE FIX: Infinite scroll pagination
  const [currentPage, setCurrentPage] = useState(0);
  const [hasMore, setHasMore] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const scrollObserverRef = useRef<IntersectionObserver | null>(null);
  const loadMoreTriggerRef = useRef<HTMLDivElement>(null);
  
  const SKILLS_PER_PAGE = 50;

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setShowDropdown(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Load categories on mount
  useEffect(() => {
    loadCategories();
  }, []);

  const loadCategories = async () => {
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch('/api/skill/categories', {
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error('Failed to load categories');
      }

      const data = await response.json();
      setCategories(data);
    } catch (err) {
      logger.error('Failed to load categories:', err);
      setError('Failed to load categories');
    }
  };

  /**
   * P1 PERFORMANCE FIX: Load skills with pagination support
   * Replaces unbounded loading with paginated approach
   */
  const loadSkills = useCallback(async (page: number = 0, append: boolean = false) => {
    if (append) {
      setIsLoadingMore(true);
    } else {
      setIsSearching(true);
    }
    
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const params = new URLSearchParams();

      if (searchTerm) {
        params.append('searchTerm', searchTerm);
      }
      if (selectedCategory) {
        params.append('category', selectedCategory);
      }
      params.append('take', SKILLS_PER_PAGE.toString());
      params.append('skip', (page * SKILLS_PER_PAGE).toString());

      const response = await fetch(`/api/skill?${params.toString()}`, {
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error('Failed to load skills');
      }

      const data = await response.json();
      const newSkills = data.skills || [];
      
      // P1 FIX: Append or replace skills based on pagination
      if (append) {
        setAvailableSkills(prev => [...prev, ...newSkills]);
      } else {
        setAvailableSkills(newSkills);
      }
      
      // P1 FIX: Check if there are more skills to load
      setHasMore(newSkills.length === SKILLS_PER_PAGE);
      setCurrentPage(page);
      setError(null);
    } catch (err) {
      logger.error('Failed to load skills:', err);
      setError('Failed to load skills');
    } finally {
      setLoading(false);
      setIsSearching(false);
      setIsLoadingMore(false);
    }
  }, [searchTerm, selectedCategory, SKILLS_PER_PAGE]);

  /**
   * P1 PERFORMANCE FIX: Load more skills when user scrolls to bottom
   */
  const loadMoreSkills = useCallback(() => {
    if (!isLoadingMore && hasMore && !loading) {
      loadSkills(currentPage + 1, true);
    }
  }, [currentPage, hasMore, isLoadingMore, loading, loadSkills]);

  // P1 PERFORMANCE FIX: Setup intersection observer for infinite scroll
  useEffect(() => {
    if (!loadMoreTriggerRef.current) return;

    // Create intersection observer to detect when user scrolls to bottom
    scrollObserverRef.current = new IntersectionObserver(
      (entries) => {
        const [entry] = entries;
        if (entry.isIntersecting && hasMore && !isLoadingMore) {
          loadMoreSkills();
        }
      },
      {
        root: null,
        rootMargin: '100px', // Start loading 100px before reaching the bottom
        threshold: 0.1
      }
    );

    scrollObserverRef.current.observe(loadMoreTriggerRef.current);

    return () => {
      if (scrollObserverRef.current) {
        scrollObserverRef.current.disconnect();
      }
    };
  }, [hasMore, isLoadingMore, loadMoreSkills]);

  // Load skills with debouncing - reset pagination on search
  useEffect(() => {
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }

    searchTimeoutRef.current = setTimeout(() => {
      // P1 FIX: Reset pagination when search changes
      setCurrentPage(0);
      setHasMore(true);
      loadSkills(0, false);
    }, 300);

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchTerm, selectedCategory]); // loadSkills intentionally excluded to prevent infinite loops

  const handleAddSkill = (skill: Skill) => {
    // Check if skill is already selected
    if (selectedSkills.some(s => s.skillId === skill.id)) {
      return;
    }

    // Check max skills limit
    if (selectedSkills.length >= maxSkills) {
      return;
    }

    const newSkill: SelectedSkill = {
      skillId: skill.id,
      skillName: skill.name,
      category: skill.category,
      proficiency: 'Beginner'
    };

    onSkillsChange([...selectedSkills, newSkill]);
    setSearchTerm('');
    setShowDropdown(false);
  };

  const handleRemoveSkill = (skillId: string) => {
    onSkillsChange(selectedSkills.filter(s => s.skillId !== skillId));
  };

  const handleUpdateSkill = (skillId: string, updates: Partial<SelectedSkill>) => {
    onSkillsChange(
      selectedSkills.map(skill =>
        skill.skillId === skillId ? { ...skill, ...updates } : skill
      )
    );
  };

  const handleSearchFocus = () => {
    setShowDropdown(true);
  };

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(e.target.value);
    setShowDropdown(true);
  };

  // Filter out already selected skills
  const filteredSkills = availableSkills.filter(
    skill => !selectedSkills.some(s => s.skillId === skill.id)
  );

  const isMinSkillsMet = selectedSkills.length >= minSkills;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h3 className="text-lg font-medium text-foreground">Skills</h3>
        <p className="text-sm text-muted-foreground mt-1">
          Add your professional skills and set your proficiency level
        </p>
      </div>

      {/* Skill Count Indicator */}
      <div className="flex items-center justify-between">
        <div className="text-sm">
          <span className={selectedSkills.length >= minSkills ? 'text-success' : 'text-muted-foreground'}>
            {selectedSkills.length} / {minSkills} skills selected
          </span>
          {minSkills > 0 && !isMinSkillsMet && (
            <span className="text-muted-foreground ml-2">(minimum required)</span>
          )}
        </div>
      </div>

      {/* Search and Filter Controls */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="relative" ref={dropdownRef}>
          <label htmlFor="skill-search" className="block text-sm font-medium text-foreground mb-1">
            Search Skills
          </label>
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-5 w-5" />
            <input
              id="skill-search"
              type="text"
              placeholder="Search skills..."
              value={searchTerm}
              onChange={handleSearchChange}
              onFocus={handleSearchFocus}
              className="w-full pl-10 pr-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring bg-background text-foreground"
            />
          </div>

          {/* Dropdown */}
          {showDropdown && (
            <div className="absolute z-10 w-full mt-1 bg-card border border-border rounded-md shadow-lg max-h-60 overflow-y-auto">
              {loading ? (
                <div className="px-4 py-3 text-sm text-muted-foreground">Loading skills...</div>
              ) : error ? (
                <div className="px-4 py-3 text-sm text-destructive">{error}</div>
              ) : filteredSkills.length === 0 ? (
                <div className="px-4 py-3 text-sm text-muted-foreground">No skills found</div>
              ) : (
                filteredSkills.map(skill => (
                  <button
                    key={skill.id}
                    type="button"
                    onClick={() => handleAddSkill(skill)}
                    className="w-full text-left px-4 py-3 hover:bg-muted focus:bg-muted focus:outline-none border-b border-border last:border-b-0"
                  >
                    <div className="font-medium text-foreground">{skill.name}</div>
                    <div className="text-sm text-muted-foreground">{skill.category}</div>
                    {skill.description && (
                      <div className="text-xs text-muted-foreground mt-1">{skill.description}</div>
                    )}
                  </button>
                ))
              )}
            </div>
          )}
        </div>

        <div>
          <label htmlFor="category-filter" className="block text-sm font-medium text-foreground mb-1">
            Category Filter
          </label>
          <select
            id="category-filter"
            value={selectedCategory}
            onChange={(e) => setSelectedCategory(e.target.value)}
            className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring bg-background text-foreground"
          >
            <option value="">All Categories</option>
            {categories.map(category => (
              <option key={category.name} value={category.name}>
                {category.name} ({category.skillCount})
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Validation Message */}
      {minSkills > 0 && !isMinSkillsMet && (
        <div className="flex items-center gap-2 p-3 bg-warning/10 border border-warning/20 rounded-md">
          <AlertCircle className="h-5 w-5 text-warning" />
          <p className="text-sm text-warning">
            Please select at least {minSkills} skills to continue
          </p>
        </div>
      )}

      {/* Error Message */}
      {error && (
        <div className="flex items-center gap-2 p-3 bg-destructive/10 border border-destructive/20 rounded-md">
          <AlertCircle className="h-5 w-5 text-destructive" />
          <p className="text-sm text-destructive">{error}</p>
        </div>
      )}

      {/* Selected Skills */}
      {selectedSkills.length > 0 && (
        <div className="space-y-4">
          <h4 className="text-md font-medium text-foreground">Selected Skills</h4>
          <div className="grid grid-cols-1 gap-4">
            {selectedSkills.map(skill => (
              <div
                key={skill.skillId}
                className="bg-card border border-border rounded-lg p-4 shadow-sm"
              >
                <div className="flex items-start justify-between mb-3">
                  <div className="flex-1">
                    <h5 className="font-medium text-foreground">{skill.skillName}</h5>
                    <p className="text-sm text-muted-foreground">{skill.category}</p>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleRemoveSkill(skill.skillId)}
                    className="text-muted-foreground hover:text-destructive focus:outline-none focus:text-destructive"
                    aria-label={`Remove ${skill.skillName}`}
                  >
                    <X className="h-5 w-5" />
                  </button>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div>
                    <label
                      htmlFor={`proficiency-${skill.skillId}`}
                      className="block text-xs font-medium text-foreground mb-1"
                    >
                      Proficiency Level
                    </label>
                    <select
                      id={`proficiency-${skill.skillId}`}
                      value={skill.proficiency}
                      onChange={(e) =>
                        handleUpdateSkill(skill.skillId, {
                          proficiency: e.target.value as SelectedSkill['proficiency']
                        })
                      }
                      className="w-full px-3 py-2 text-sm border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring bg-background text-foreground"
                    >
                      {proficiencyLevels.map(level => (
                        <option key={level} value={level}>
                          {level} - {proficiencyDescriptions[level]}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label
                      htmlFor={`years-${skill.skillId}`}
                      className="block text-xs font-medium text-foreground mb-1"
                    >
                      Years of Experience (Optional)
                    </label>
                    <input
                      id={`years-${skill.skillId}`}
                      type="number"
                      min="0"
                      max="50"
                      value={skill.yearsOfExperience || ''}
                      onChange={(e) =>
                        handleUpdateSkill(skill.skillId, {
                          yearsOfExperience: e.target.value ? parseInt(e.target.value) : undefined
                        })
                      }
                      className="w-full px-3 py-2 text-sm border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring bg-background text-foreground"
                      placeholder="e.g., 3"
                    />
                  </div>
                </div>

                {/* Proficiency Badge */}
                <div className="mt-3">
                  <span
                    className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${proficiencyColors[skill.proficiency]}`}
                  >
                    {skill.proficiency}
                    {skill.yearsOfExperience && ` • ${skill.yearsOfExperience}y`}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Empty State */}
      {selectedSkills.length === 0 && (
        <div className="text-center py-8 border-2 border-dashed border-border rounded-lg">
          <p className="text-muted-foreground">No skills selected yet</p>
          <p className="text-sm text-muted-foreground mt-1">Search and select skills to get started</p>
        </div>
      )}
    </div>
  );
}
