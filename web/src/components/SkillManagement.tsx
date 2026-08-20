'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect } from 'react';
import { Plus, Search, Edit2, Trash2, Star, Users, Filter, Eye, EyeOff, Loader2 } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface Skill {
  id: string;
  name: string;
  description?: string;
  category: string;
  isSystemManaged: boolean;
  isActive: boolean;
  createdAt: string;
}

interface UserSkill {
  id: string;
  userId: string;
  skillId: string;
  skill: Skill;
  proficiency: 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert';
  yearsOfExperience?: number;
  notes?: string;
  isVisible: boolean;
  createdAt: string;
  endorsements: SkillEndorsement[];
}

interface SkillEndorsement {
  id: string;
  userSkillId: string;
  endorsedByUser: {
    id: string;
    displayName: string;
    title?: string;
    company?: string;
    avatarUrl?: string;
  };
  comment?: string;
  isVisible: boolean;
  createdAt: string;
}

interface SkillCategory {
  name: string;
  skillCount: number;
  userCount: number;
}

type ProficiencyLevel = 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert';

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

export default function SkillManagement() {
  const [userSkills, setUserSkills] = useState<UserSkill[]>([]);
  const [availableSkills, setAvailableSkills] = useState<Skill[]>([]);
  const [categories, setCategories] = useState<SkillCategory[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('');
  const [showOnlyVisible, setShowOnlyVisible] = useState(false);
  const [isAddingSkill, setIsAddingSkill] = useState(false);
  const [editingSkill, setEditingSkill] = useState<UserSkill | null>(null);
  const [newSkill, setNewSkill] = useState({
    skillId: '',
    proficiency: 'Beginner' as ProficiencyLevel,
    yearsOfExperience: undefined as number | undefined,
    notes: '',
    isVisible: true
  });

  useEffect(() => {
    loadUserSkills();
    loadAvailableSkills();
    loadCategories();
  }, []);

  const loadUserSkills = async () => {
    try {
      // Mock data - replace with actual API call
      const mockUserSkills: UserSkill[] = [
        {
          id: '1',
          userId: 'user1',
          skillId: 'skill1',
          skill: {
            id: 'skill1',
            name: 'React',
            description: 'JavaScript library for building user interfaces',
            category: 'Frontend Development',
            isSystemManaged: true,
            isActive: true,
            createdAt: '2023-01-01T00:00:00Z'
          },
          proficiency: 'Advanced',
          yearsOfExperience: 4,
          notes: 'Experienced with hooks, context, and performance optimization',
          isVisible: true,
          createdAt: '2023-01-01T00:00:00Z',
          endorsements: [
            {
              id: 'e1',
              userSkillId: '1',
              endorsedByUser: {
                id: 'user2',
                displayName: 'John Smith',
                title: 'Senior Developer',
                company: 'TechCorp'
              },
              comment: 'Excellent React skills, helped our team significantly',
              isVisible: true,
              createdAt: '2023-02-01T00:00:00Z'
            }
          ]
        },
        {
          id: '2',
          userId: 'user1',
          skillId: 'skill2',
          skill: {
            id: 'skill2',
            name: 'TypeScript',
            description: 'Typed superset of JavaScript',
            category: 'Programming Languages',
            isSystemManaged: true,
            isActive: true,
            createdAt: '2023-01-01T00:00:00Z'
          },
          proficiency: 'Expert',
          yearsOfExperience: 5,
          notes: 'Deep understanding of type systems and generics',
          isVisible: true,
          createdAt: '2023-01-01T00:00:00Z',
          endorsements: []
        }
      ];
      setUserSkills(mockUserSkills);
    } catch (error) {
      logger.error('Failed to load user skills', error, { component: 'SkillManagement' });
    }
  };

  const loadAvailableSkills = async () => {
    try {
      // Mock data - replace with actual API call
      const mockSkills: Skill[] = [
        {
          id: 'skill3',
          name: 'Next.js',
          description: 'React framework for production',
          category: 'Frontend Development',
          isSystemManaged: true,
          isActive: true,
          createdAt: '2023-01-01T00:00:00Z'
        },
        {
          id: 'skill4',
          name: 'Node.js',
          description: 'JavaScript runtime for server-side development',
          category: 'Backend Development',
          isSystemManaged: true,
          isActive: true,
          createdAt: '2023-01-01T00:00:00Z'
        }
      ];
      setAvailableSkills(mockSkills);
    } catch (error) {
      logger.error('Failed to load available skills', error, { component: 'SkillManagement' });
    }
  };

  const loadCategories = async () => {
    try {
      // Mock data - replace with actual API call
      const mockCategories: SkillCategory[] = [
        { name: 'Frontend Development', skillCount: 25, userCount: 150 },
        { name: 'Backend Development', skillCount: 30, userCount: 120 },
        { name: 'Programming Languages', skillCount: 20, userCount: 200 },
        { name: 'DevOps', skillCount: 15, userCount: 80 },
        { name: 'Database', skillCount: 12, userCount: 90 }
      ];
      setCategories(mockCategories);
      setLoading(false);
    } catch (error) {
      logger.error('Failed to load categories', error, { component: 'SkillManagement' });
      setLoading(false);
    }
  };

  const handleAddSkill = async () => {
    if (!newSkill.skillId) return;

    try {
      // Mock API call - replace with actual implementation
      const selectedSkill = availableSkills.find(skill => skill.id === newSkill.skillId);
      if (!selectedSkill) return;

      const newUserSkill: UserSkill = {
        id: Date.now().toString(),
        userId: 'user1',
        skillId: newSkill.skillId,
        skill: selectedSkill,
        proficiency: newSkill.proficiency,
        yearsOfExperience: newSkill.yearsOfExperience,
        notes: newSkill.notes,
        isVisible: newSkill.isVisible,
        createdAt: new Date().toISOString(),
        endorsements: []
      };

      setUserSkills([...userSkills, newUserSkill]);
      setNewSkill({
        skillId: '',
        proficiency: 'Beginner',
        yearsOfExperience: undefined,
        notes: '',
        isVisible: true
      });
      setIsAddingSkill(false);
    } catch (error) {
      logger.error('Failed to add skill', error, { component: 'SkillManagement' });
    }
  };

  const handleUpdateSkill = async (updatedSkill: UserSkill) => {
    try {
      // Mock API call - replace with actual implementation
      setUserSkills(userSkills.map(skill => 
        skill.id === updatedSkill.id ? updatedSkill : skill
      ));
      setEditingSkill(null);
    } catch (error) {
      logger.error('Failed to update skill', error, { component: 'SkillManagement' });
    }
  };

  const handleDeleteSkill = async (skillId: string) => {
    if (!confirm('Are you sure you want to remove this skill from your profile?')) {
      return;
    }

    try {
      // Mock API call - replace with actual implementation
      setUserSkills(userSkills.filter(skill => skill.id !== skillId));
    } catch (error) {
      logger.error('Failed to delete skill', error, { component: 'SkillManagement' });
    }
  };

  const toggleSkillVisibility = async (skill: UserSkill) => {
    try {
      const updatedSkill = { ...skill, isVisible: !skill.isVisible };
      await handleUpdateSkill(updatedSkill);
    } catch (error) {
      logger.error('Failed to toggle skill visibility', error, { component: 'SkillManagement' });
    }
  };

  const filteredSkills = userSkills.filter(skill => {
    const matchesSearch = skill.skill.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         skill.notes?.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesCategory = !selectedCategory || skill.skill.category === selectedCategory;
    const matchesVisibility = !showOnlyVisible || skill.isVisible;
    
    return matchesSearch && matchesCategory && matchesVisibility;
  });

  const availableSkillsForAdd = availableSkills.filter(skill => 
    !userSkills.some(userSkill => userSkill.skillId === skill.id) &&
    skill.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  if (loading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-foreground mb-2">Skill Management</h1>
        <p className="text-muted-foreground">Manage your professional skills and showcase your expertise</p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
        <div className="bg-card p-6 rounded-lg shadow">
          <div className="flex items-center">
            <Star className="h-8 w-8 text-warning" />
            <div className="ml-4">
              <p className="text-sm font-medium text-muted-foreground">Total Skills</p>
              <p className="text-2xl font-semibold text-foreground">{userSkills.length}</p>
            </div>
          </div>
        </div>

        <div className="bg-card p-6 rounded-lg shadow">
          <div className="flex items-center">
            <Users className="h-8 w-8 text-primary" />
            <div className="ml-4">
              <p className="text-sm font-medium text-muted-foreground">Endorsements</p>
              <p className="text-2xl font-semibold text-foreground">
                {userSkills.reduce((total, skill) => total + skill.endorsements.length, 0)}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-card p-6 rounded-lg shadow">
          <div className="flex items-center">
            <Eye className="h-8 w-8 text-success" />
            <div className="ml-4">
              <p className="text-sm font-medium text-muted-foreground">Visible Skills</p>
              <p className="text-2xl font-semibold text-foreground">
                {userSkills.filter(skill => skill.isVisible).length}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-card p-6 rounded-lg shadow">
          <div className="flex items-center">
            <Filter className="h-8 w-8 text-accent" />
            <div className="ml-4">
              <p className="text-sm font-medium text-muted-foreground">Categories</p>
              <p className="text-2xl font-semibold text-foreground">
                {new Set(userSkills.map(skill => skill.skill.category)).size}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Controls */}
      {/* Controls */}
      <Card className="mb-6">
        <CardContent className="p-6">
          <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
            <div className="flex flex-col sm:flex-row gap-4 flex-1">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
                <Input
                  type="text"
                  placeholder="Search skills..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                />
              </div>

              <select
                value={selectedCategory}
                onChange={(e) => setSelectedCategory(e.target.value)}
                className="px-4 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
              >
                <option value="">All Categories</option>
                {categories.map(category => (
                  <option key={category.name} value={category.name}>
                    {category.name} ({category.skillCount})
                  </option>
                ))}
              </select>

              <label className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  checked={showOnlyVisible}
                  onChange={(e) => setShowOnlyVisible(e.target.checked)}
                  className="rounded border-input text-primary focus:ring-ring"
                />
                <span className="text-sm">Visible only</span>
              </label>
            </div>

            <Button onClick={() => setIsAddingSkill(true)}>
              <Plus className="h-4 w-4 mr-2" />
              Add Skill
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Skills Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {filteredSkills.map(skill => (
          <Card key={skill.id}>
            <CardContent className="p-6">
            <div className="flex items-start justify-between mb-4">
              <div className="flex-1">
                <h3 className="text-lg font-semibold text-foreground mb-1">
                  {skill.skill.name}
                </h3>
                <p className="text-sm text-muted-foreground mb-2">{skill.skill.category}</p>
                <Badge variant="outline" className={proficiencyColors[skill.proficiency]}>
                  {skill.proficiency}
                  {skill.yearsOfExperience && ` • ${skill.yearsOfExperience}y`}
                </Badge>
              </div>
              
              <div className="flex items-center space-x-2">
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => toggleSkillVisibility(skill)}
                  title={skill.isVisible ? 'Hide from profile' : 'Show on profile'}
                >
                  {skill.isVisible ? <Eye className="h-4 w-4" /> : <EyeOff className="h-4 w-4" />}
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => setEditingSkill(skill)}
                  title="Edit skill"
                >
                  <Edit2 className="h-4 w-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => handleDeleteSkill(skill.id)}
                  title="Remove skill"
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </div>

            {skill.notes && (
              <p className="text-sm mb-4">{skill.notes}</p>
            )}

            {skill.endorsements.length > 0 && (
              <div className="mb-4">
                <p className="text-xs font-medium text-muted-foreground mb-2">
                  {skill.endorsements.length} Endorsement{skill.endorsements.length !== 1 ? 's' : ''}
                </p>
                <div className="space-y-2">
                  {skill.endorsements.slice(0, 2).map(endorsement => (
                    <div key={endorsement.id} className="bg-muted/50 p-2 rounded">
                      <p className="text-xs font-medium">
                        {endorsement.endorsedByUser.displayName}
                      </p>
                      {endorsement.comment && (
                        <p className="text-xs text-muted-foreground mt-1">{endorsement.comment}</p>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="text-xs text-muted-foreground">
              Added {new Date(skill.createdAt).toLocaleDateString()}
            </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {filteredSkills.length === 0 && (
        <div className="text-center py-12">
          <Star className="mx-auto h-12 w-12 text-muted-foreground" />
          <h3 className="mt-2 text-sm font-medium text-foreground">No skills found</h3>
          <p className="mt-1 text-sm text-muted-foreground">
            {searchTerm || selectedCategory || showOnlyVisible
              ? 'Try adjusting your filters or search term.'
              : 'Get started by adding your first skill.'}
          </p>
        </div>
      )}

      {/* Add Skill Modal */}
      {isAddingSkill && (
        <div className="fixed inset-0 bg-background/80 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border border-border w-96 shadow-lg rounded-md bg-card">
            <h3 className="text-lg font-bold text-foreground mb-4">Add New Skill</h3>
            
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  Skill
                </label>
                <select
                  value={newSkill.skillId}
                  onChange={(e) => setNewSkill({...newSkill, skillId: e.target.value})}
                  className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                >
                  <option value="">Select a skill...</option>
                  {availableSkillsForAdd.map(skill => (
                    <option key={skill.id} value={skill.id}>
                      {skill.name} ({skill.category})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  Proficiency Level
                </label>
                <select
                  value={newSkill.proficiency}
                  onChange={(e) => setNewSkill({...newSkill, proficiency: e.target.value as ProficiencyLevel})}
                  className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                >
                  {Object.entries(proficiencyDescriptions).map(([level, description]) => (
                    <option key={level} value={level}>
                      {level} - {description}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  Years of Experience (optional)
                </label>
                <input
                  type="number"
                  min="0"
                  max="50"
                  value={newSkill.yearsOfExperience || ''}
                  onChange={(e) => setNewSkill({...newSkill, yearsOfExperience: e.target.value ? parseInt(e.target.value) : undefined})}
                  className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  placeholder="e.g., 3"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  Notes (optional)
                </label>
                <textarea
                  value={newSkill.notes}
                  onChange={(e) => setNewSkill({...newSkill, notes: e.target.value})}
                  className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  rows={3}
                  placeholder="Add any additional context or achievements..."
                />
              </div>

              <div className="flex items-center">
                <input
                  type="checkbox"
                  id="visible"
                  checked={newSkill.isVisible}
                  onChange={(e) => setNewSkill({...newSkill, isVisible: e.target.checked})}
                  className="rounded border-input text-primary focus:ring-ring"
                />
                <label htmlFor="visible" className="ml-2 text-sm text-foreground">
                  Visible on public profile
                </label>
              </div>
            </div>

            <div className="flex justify-end space-x-3 mt-6">
              <button
                onClick={() => setIsAddingSkill(false)}
                className="px-4 py-2 text-sm font-medium text-foreground bg-muted hover:bg-muted/80 rounded-full"
              >
                Cancel
              </button>
              <button
                onClick={handleAddSkill}
                disabled={!newSkill.skillId}
                className="px-4 py-2 text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 disabled:bg-muted disabled:text-muted-foreground rounded-full"
              >
                Add Skill
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Edit Skill Modal */}
      {editingSkill && (
        <div className="fixed inset-0 bg-background/80 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border border-border w-96 shadow-lg rounded-md bg-card">
            <h3 className="text-lg font-bold text-foreground mb-4">Edit {editingSkill.skill.name}</h3>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  Proficiency Level
                </label>
                <select
                  value={editingSkill.proficiency}
                  onChange={(e) => setEditingSkill({...editingSkill, proficiency: e.target.value as ProficiencyLevel})}
                  className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                >
                  {Object.entries(proficiencyDescriptions).map(([level, description]) => (
                    <option key={level} value={level}>
                      {level} - {description}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  Years of Experience (optional)
                </label>
                <input
                  type="number"
                  min="0"
                  max="50"
                  value={editingSkill.yearsOfExperience || ''}
                  onChange={(e) => setEditingSkill({...editingSkill, yearsOfExperience: e.target.value ? parseInt(e.target.value) : undefined})}
                  className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  placeholder="e.g., 3"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  Notes (optional)
                </label>
                <textarea
                  value={editingSkill.notes || ''}
                  onChange={(e) => setEditingSkill({...editingSkill, notes: e.target.value})}
                  className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  rows={3}
                  placeholder="Add any additional context or achievements..."
                />
              </div>

              <div className="flex items-center">
                <input
                  type="checkbox"
                  id="editVisible"
                  checked={editingSkill.isVisible}
                  onChange={(e) => setEditingSkill({...editingSkill, isVisible: e.target.checked})}
                  className="rounded border-input text-primary focus:ring-ring"
                />
                <label htmlFor="editVisible" className="ml-2 text-sm text-foreground">
                  Visible on public profile
                </label>
              </div>
            </div>

            <div className="flex justify-end space-x-3 mt-6">
              <button
                onClick={() => setEditingSkill(null)}
                className="px-4 py-2 text-sm font-medium text-foreground bg-muted hover:bg-muted/80 rounded-full"
              >
                Cancel
              </button>
              <button
                onClick={() => handleUpdateSkill(editingSkill)}
                className="px-4 py-2 text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 rounded-full"
              >
                Save Changes
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}