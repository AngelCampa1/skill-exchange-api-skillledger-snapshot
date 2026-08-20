'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect } from 'react';
import { 
  Plus, 
  Edit2, 
  Trash2, 
  Calendar, 
  MapPin, 
  Building, 
  GraduationCap, 
  Code,
  Users,
  Award,
  Eye,
  EyeOff,
  Star,
  Filter,
  Search
} from 'lucide-react';

interface Skill {
  id: string;
  name: string;
  description?: string;
  category: string;
}

interface Experience {
  id: string;
  userId: string;
  type: 'Work' | 'Education' | 'Project' | 'Volunteer';
  title: string;
  organization: string;
  location?: string;
  description?: string;
  startDate: string;
  endDate?: string;
  isCurrent: boolean;
  isVisible: boolean;
  isFeatured: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
  skills: Skill[];
  durationInMonths: number;
  durationDisplay: string;
}

type ExperienceType = 'Work' | 'Education' | 'Project' | 'Volunteer';

const experienceTypeIcons = {
  Work: Building,
  Education: GraduationCap,
  Project: Code,
  Volunteer: Users
};

const experienceTypeColors = {
  Work: 'bg-primary/10 text-primary border-primary/20',
  Education: 'bg-success/10 text-success border-success/20',
  Project: 'bg-accent/10 text-accent border-accent/20',
  Volunteer: 'bg-warning/10 text-warning border-warning/20'
};

export default function ExperienceTimeline() {
  const [experiences, setExperiences] = useState<Experience[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedType, setSelectedType] = useState<ExperienceType | ''>('');
  const [showOnlyVisible, setShowOnlyVisible] = useState(false);
  const [showOnlyFeatured, setShowOnlyFeatured] = useState(false);
  const [isAddingExperience, setIsAddingExperience] = useState(false);
  const [editingExperience, setEditingExperience] = useState<Experience | null>(null);
  const [newExperience, setNewExperience] = useState({
    type: 'Work' as ExperienceType,
    title: '',
    organization: '',
    location: '',
    description: '',
    startDate: '',
    endDate: '',
    isCurrent: false,
    isVisible: true,
    isFeatured: false
  });

  useEffect(() => {
    loadExperiences();
  }, []);

  const loadExperiences = async () => {
    try {
      // Mock data - replace with actual API call
      const mockExperiences: Experience[] = [
        {
          id: '1',
          userId: 'user1',
          type: 'Work',
          title: 'Senior Full Stack Developer',
          organization: 'TechCorp Inc.',
          location: 'San Francisco, CA',
          description: 'Led development of scalable web applications using React, Node.js, and PostgreSQL. Mentored junior developers and collaborated with cross-functional teams to deliver high-quality software solutions.',
          startDate: '2022-01-01T00:00:00Z',
          endDate: undefined,
          isCurrent: true,
          isVisible: true,
          isFeatured: true,
          displayOrder: 1,
          createdAt: '2022-01-01T00:00:00Z',
          updatedAt: '2022-01-01T00:00:00Z',
          skills: [
            { id: 's1', name: 'React', category: 'Frontend' },
            { id: 's2', name: 'Node.js', category: 'Backend' },
            { id: 's3', name: 'PostgreSQL', category: 'Database' }
          ],
          durationInMonths: 24,
          durationDisplay: '2 years'
        },
        {
          id: '2',
          userId: 'user1',
          type: 'Work',
          title: 'Frontend Developer',
          organization: 'StartupXYZ',
          location: 'Remote',
          description: 'Developed responsive web applications using React and TypeScript. Collaborated with UX designers to create intuitive user interfaces and optimized applications for performance.',
          startDate: '2020-03-01T00:00:00Z',
          endDate: '2021-12-31T00:00:00Z',
          isCurrent: false,
          isVisible: true,
          isFeatured: false,
          displayOrder: 2,
          createdAt: '2020-03-01T00:00:00Z',
          updatedAt: '2020-03-01T00:00:00Z',
          skills: [
            { id: 's1', name: 'React', category: 'Frontend' },
            { id: 's4', name: 'TypeScript', category: 'Language' },
            { id: 's5', name: 'CSS', category: 'Frontend' }
          ],
          durationInMonths: 22,
          durationDisplay: '1 year 10 months'
        },
        {
          id: '3',
          userId: 'user1',
          type: 'Education',
          title: 'Bachelor of Science in Computer Science',
          organization: 'University of California, Berkeley',
          location: 'Berkeley, CA',
          description: 'Comprehensive computer science education with focus on algorithms, data structures, software engineering, and system design. Graduated Magna Cum Laude.',
          startDate: '2016-08-01T00:00:00Z',
          endDate: '2020-05-01T00:00:00Z',
          isCurrent: false,
          isVisible: true,
          isFeatured: true,
          displayOrder: 3,
          createdAt: '2016-08-01T00:00:00Z',
          updatedAt: '2016-08-01T00:00:00Z',
          skills: [
            { id: 's6', name: 'Java', category: 'Language' },
            { id: 's7', name: 'Python', category: 'Language' },
            { id: 's8', name: 'Algorithms', category: 'Computer Science' }
          ],
          durationInMonths: 45,
          durationDisplay: '3 years 9 months'
        },
        {
          id: '4',
          userId: 'user1',
          type: 'Project',
          title: 'Open Source Contribution',
          organization: 'React Community',
          location: 'Remote',
          description: 'Contributed to React ecosystem by developing and maintaining a popular component library with over 10k weekly downloads on npm.',
          startDate: '2021-01-01T00:00:00Z',
          endDate: '2022-06-01T00:00:00Z',
          isCurrent: false,
          isVisible: true,
          isFeatured: false,
          displayOrder: 4,
          createdAt: '2021-01-01T00:00:00Z',
          updatedAt: '2021-01-01T00:00:00Z',
          skills: [
            { id: 's1', name: 'React', category: 'Frontend' },
            { id: 's4', name: 'TypeScript', category: 'Language' },
            { id: 's9', name: 'Open Source', category: 'Development' }
          ],
          durationInMonths: 17,
          durationDisplay: '1 year 5 months'
        }
      ];
      
      // Sort by display order and start date
      mockExperiences.sort((a, b) => {
        if (a.displayOrder !== b.displayOrder) {
          return a.displayOrder - b.displayOrder;
        }
        return new Date(b.startDate).getTime() - new Date(a.startDate).getTime();
      });
      
      setExperiences(mockExperiences);
      setLoading(false);
    } catch (error) {
      logger.error('Failed to load experiences', error, { component: 'ExperienceTimeline' });
      setLoading(false);
    }
  };

  const handleAddExperience = async () => {
    if (!newExperience.title || !newExperience.organization || !newExperience.startDate) {
      return;
    }

    try {
      // Mock API call - replace with actual implementation
      const experience: Experience = {
        id: Date.now().toString(),
        userId: 'user1',
        ...newExperience,
        displayOrder: experiences.length + 1,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        skills: [],
        durationInMonths: calculateDuration(newExperience.startDate, newExperience.endDate),
        durationDisplay: formatDuration(calculateDuration(newExperience.startDate, newExperience.endDate))
      };

      setExperiences([experience, ...experiences]);
      setNewExperience({
        type: 'Work',
        title: '',
        organization: '',
        location: '',
        description: '',
        startDate: '',
        endDate: '',
        isCurrent: false,
        isVisible: true,
        isFeatured: false
      });
      setIsAddingExperience(false);
    } catch (error) {
      logger.error('Failed to add experience', error, { component: 'ExperienceTimeline' });
    }
  };

  const handleUpdateExperience = async (updatedExperience: Experience) => {
    try {
      // Mock API call - replace with actual implementation
      const updated = {
        ...updatedExperience,
        updatedAt: new Date().toISOString(),
        durationInMonths: calculateDuration(updatedExperience.startDate, updatedExperience.endDate),
        durationDisplay: formatDuration(calculateDuration(updatedExperience.startDate, updatedExperience.endDate))
      };

      setExperiences(experiences.map(exp => 
        exp.id === updated.id ? updated : exp
      ));
      setEditingExperience(null);
    } catch (error) {
      logger.error('Failed to update experience', error, { component: 'ExperienceTimeline' });
    }
  };

  const handleDeleteExperience = async (experienceId: string) => {
    if (!confirm('Are you sure you want to delete this experience?')) {
      return;
    }

    try {
      // Mock API call - replace with actual implementation
      setExperiences(experiences.filter(exp => exp.id !== experienceId));
    } catch (error) {
      logger.error('Failed to delete experience', error, { component: 'ExperienceTimeline' });
    }
  };

  const toggleExperienceVisibility = async (experience: Experience) => {
    try {
      const updated = { ...experience, isVisible: !experience.isVisible };
      await handleUpdateExperience(updated);
    } catch (error) {
      logger.error('Failed to toggle experience visibility', error, { component: 'ExperienceTimeline' });
    }
  };

  const toggleExperienceFeatured = async (experience: Experience) => {
    try {
      const updated = { ...experience, isFeatured: !experience.isFeatured };
      await handleUpdateExperience(updated);
    } catch (error) {
      logger.error('Failed to toggle experience featured status', error, { component: 'ExperienceTimeline' });
    }
  };

  const calculateDuration = (startDate: string, endDate?: string): number => {
    const start = new Date(startDate);
    const end = endDate ? new Date(endDate) : new Date();
    
    const monthDiff = (end.getFullYear() - start.getFullYear()) * 12 + 
                     (end.getMonth() - start.getMonth());
    
    return Math.max(0, monthDiff);
  };

  const formatDuration = (months: number): string => {
    if (months === 0) return 'Less than a month';
    
    const years = Math.floor(months / 12);
    const remainingMonths = months % 12;
    
    if (years === 0) return `${months} month${months === 1 ? '' : 's'}`;
    if (remainingMonths === 0) return `${years} year${years === 1 ? '' : 's'}`;
    
    return `${years} year${years === 1 ? '' : 's'} ${remainingMonths} month${remainingMonths === 1 ? '' : 's'}`;
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      year: 'numeric'
    });
  };

  const filteredExperiences = experiences.filter(experience => {
    const matchesSearch = experience.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         experience.organization.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         experience.description?.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesType = !selectedType || experience.type === selectedType;
    const matchesVisibility = !showOnlyVisible || experience.isVisible;
    const matchesFeatured = !showOnlyFeatured || experience.isFeatured;
    
    return matchesSearch && matchesType && matchesVisibility && matchesFeatured;
  });

  if (loading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-foreground mb-2">Experience Timeline</h1>
        <p className="text-muted-foreground">Manage your professional experiences and showcase your career journey</p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
        <div className="bg-card p-6 rounded-lg shadow">
          <div className="flex items-center">
            <Building className="h-8 w-8 text-primary" />
            <div className="ml-4">
              <p className="text-sm font-medium text-muted-foreground">Total Experiences</p>
              <p className="text-2xl font-semibold text-foreground">{experiences.length}</p>
            </div>
          </div>
        </div>

        <div className="bg-card p-6 rounded-lg shadow">
          <div className="flex items-center">
            <Star className="h-8 w-8 text-warning" />
            <div className="ml-4">
              <p className="text-sm font-medium text-muted-foreground">Featured</p>
              <p className="text-2xl font-semibold text-foreground">
                {experiences.filter(exp => exp.isFeatured).length}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-card p-6 rounded-lg shadow">
          <div className="flex items-center">
            <Eye className="h-8 w-8 text-success" />
            <div className="ml-4">
              <p className="text-sm font-medium text-muted-foreground">Visible</p>
              <p className="text-2xl font-semibold text-foreground">
                {experiences.filter(exp => exp.isVisible).length}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-card p-6 rounded-lg shadow">
          <div className="flex items-center">
            <Calendar className="h-8 w-8 text-accent" />
            <div className="ml-4">
              <p className="text-sm font-medium text-muted-foreground">Experience Types</p>
              <p className="text-2xl font-semibold text-foreground">
                {new Set(experiences.map(exp => exp.type)).size}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Controls */}
      <div className="bg-card p-6 rounded-lg shadow mb-6">
        <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
          <div className="flex flex-col sm:flex-row gap-4 flex-1">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
              <input
                type="text"
                placeholder="Search experiences..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10 pr-4 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
              />
            </div>

            <select
              value={selectedType}
              onChange={(e) => setSelectedType(e.target.value as ExperienceType | '')}
              className="px-4 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
            >
              <option value="">All Types</option>
              <option value="Work">Work</option>
              <option value="Education">Education</option>
              <option value="Project">Project</option>
              <option value="Volunteer">Volunteer</option>
            </select>

            <div className="flex items-center space-x-4">
              <label className="flex items-center">
                <input
                  type="checkbox"
                  checked={showOnlyVisible}
                  onChange={(e) => setShowOnlyVisible(e.target.checked)}
                  className="rounded border-input text-primary focus:ring-ring"
                />
                <span className="ml-2 text-sm text-foreground">Visible only</span>
              </label>

              <label className="flex items-center">
                <input
                  type="checkbox"
                  checked={showOnlyFeatured}
                  onChange={(e) => setShowOnlyFeatured(e.target.checked)}
                  className="rounded border-input text-primary focus:ring-ring"
                />
                <span className="ml-2 text-sm text-foreground">Featured only</span>
              </label>
            </div>
          </div>

          <button
            onClick={() => setIsAddingExperience(true)}
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-full shadow-sm text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring"
          >
            <Plus className="h-4 w-4 mr-2" />
            Add Experience
          </button>
        </div>
      </div>

      {/* Timeline */}
      <div className="relative">
        <div className="absolute left-8 top-0 bottom-0 w-0.5 bg-border"></div>

        <div className="space-y-6">
          {filteredExperiences.map((experience, index) => {
            const IconComponent = experienceTypeIcons[experience.type];

            return (
              <div key={experience.id} className="relative flex items-start space-x-6">
                {/* Timeline Node */}
                <div className="flex-shrink-0">
                  <div className={`w-16 h-16 rounded-full border-4 border-card shadow-lg flex items-center justify-center ${
                    experienceTypeColors[experience.type].split(' ')[0]
                  }`}>
                    <IconComponent className="h-6 w-6" />
                  </div>
                </div>

                {/* Content */}
                <div className="flex-1 bg-card rounded-lg shadow p-6">
                  <div className="flex items-start justify-between mb-4">
                    <div className="flex-1">
                      <div className="flex items-center space-x-3 mb-2">
                        <h3 className="text-xl font-semibold text-foreground">
                          {experience.title}
                        </h3>
                        <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full border ${experienceTypeColors[experience.type]}`}>
                          {experience.type}
                        </span>
                        {experience.isFeatured && (
                          <Star className="h-4 w-4 text-warning fill-current" />
                        )}
                      </div>

                      <p className="text-lg font-medium text-muted-foreground mb-1">
                        {experience.organization}
                      </p>

                      <div className="flex items-center space-x-4 text-sm text-muted-foreground mb-3">
                        <div className="flex items-center">
                          <Calendar className="h-4 w-4 mr-1" />
                          {formatDate(experience.startDate)} - {experience.isCurrent ? 'Present' : formatDate(experience.endDate!)}
                        </div>
                        <div className="flex items-center">
                          <Calendar className="h-4 w-4 mr-1" />
                          {experience.durationDisplay}
                        </div>
                        {experience.location && (
                          <div className="flex items-center">
                            <MapPin className="h-4 w-4 mr-1" />
                            {experience.location}
                          </div>
                        )}
                      </div>
                    </div>
                    
                    <div className="flex items-center space-x-2">
                      <button
                        onClick={() => toggleExperienceFeatured(experience)}
                        className={`text-muted-foreground hover:text-warning ${experience.isFeatured ? 'text-warning' : ''}`}
                        title={experience.isFeatured ? 'Remove from featured' : 'Add to featured'}
                      >
                        <Star className={`h-4 w-4 ${experience.isFeatured ? 'fill-current' : ''}`} />
                      </button>
                      <button
                        onClick={() => toggleExperienceVisibility(experience)}
                        className="text-muted-foreground hover:text-foreground"
                        title={experience.isVisible ? 'Hide from profile' : 'Show on profile'}
                      >
                        {experience.isVisible ? <Eye className="h-4 w-4" /> : <EyeOff className="h-4 w-4" />}
                      </button>
                      <button
                        onClick={() => setEditingExperience(experience)}
                        className="text-muted-foreground hover:text-primary"
                        title="Edit experience"
                      >
                        <Edit2 className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => handleDeleteExperience(experience.id)}
                        className="text-muted-foreground hover:text-destructive"
                        title="Delete experience"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </div>
                  </div>

                  {experience.description && (
                    <p className="text-foreground mb-4 leading-relaxed">
                      {experience.description}
                    </p>
                  )}

                  {experience.skills.length > 0 && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground mb-2">Skills Used:</p>
                      <div className="flex flex-wrap gap-2">
                        {experience.skills.map(skill => (
                          <span
                            key={skill.id}
                            className="inline-flex px-2 py-1 text-xs font-medium text-primary bg-primary/10 rounded-full"
                          >
                            {skill.name}
                          </span>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {filteredExperiences.length === 0 && (
        <div className="text-center py-12">
          <Building className="mx-auto h-12 w-12 text-muted-foreground" />
          <h3 className="mt-2 text-sm font-medium text-foreground">No experiences found</h3>
          <p className="mt-1 text-sm text-muted-foreground">
            {searchTerm || selectedType || showOnlyVisible || showOnlyFeatured
              ? 'Try adjusting your filters or search term.'
              : 'Get started by adding your first experience.'}
          </p>
        </div>
      )}

      {/* Add Experience Modal */}
      {isAddingExperience && (
        <div className="fixed inset-0 bg-background/80 overflow-y-auto h-full w-full z-50">
          <div className="relative top-10 mx-auto p-5 border border-border max-w-2xl shadow-lg rounded-md bg-card">
            <h3 className="text-lg font-bold text-foreground mb-4">Add New Experience</h3>
            
            <div className="space-y-4 max-h-96 overflow-y-auto">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    Type *
                  </label>
                  <select
                    value={newExperience.type}
                    onChange={(e) => setNewExperience({...newExperience, type: e.target.value as ExperienceType})}
                    className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  >
                    <option value="Work">Work</option>
                    <option value="Education">Education</option>
                    <option value="Project">Project</option>
                    <option value="Volunteer">Volunteer</option>
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    Title *
                  </label>
                  <input
                    type="text"
                    value={newExperience.title}
                    onChange={(e) => setNewExperience({...newExperience, title: e.target.value})}
                    className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                    placeholder="e.g., Senior Developer"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    Organization *
                  </label>
                  <input
                    type="text"
                    value={newExperience.organization}
                    onChange={(e) => setNewExperience({...newExperience, organization: e.target.value})}
                    className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                    placeholder="e.g., TechCorp Inc."
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    Location
                  </label>
                  <input
                    type="text"
                    value={newExperience.location}
                    onChange={(e) => setNewExperience({...newExperience, location: e.target.value})}
                    className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                    placeholder="e.g., San Francisco, CA"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  Description
                </label>
                <textarea
                  value={newExperience.description}
                  onChange={(e) => setNewExperience({...newExperience, description: e.target.value})}
                  className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  rows={4}
                  placeholder="Describe your role, responsibilities, and achievements..."
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    Start Date *
                  </label>
                  <input
                    type="date"
                    value={newExperience.startDate}
                    onChange={(e) => setNewExperience({...newExperience, startDate: e.target.value})}
                    className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    End Date
                  </label>
                  <input
                    type="date"
                    value={newExperience.endDate}
                    onChange={(e) => setNewExperience({...newExperience, endDate: e.target.value, isCurrent: !e.target.value})}
                    className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                    disabled={newExperience.isCurrent}
                  />
                </div>
              </div>

              <div className="flex items-center space-x-6">
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={newExperience.isCurrent}
                    onChange={(e) => setNewExperience({...newExperience, isCurrent: e.target.checked, endDate: e.target.checked ? '' : newExperience.endDate})}
                    className="rounded border-input text-primary focus:ring-ring"
                  />
                  <span className="ml-2 text-sm text-foreground">Currently active</span>
                </label>

                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={newExperience.isVisible}
                    onChange={(e) => setNewExperience({...newExperience, isVisible: e.target.checked})}
                    className="rounded border-input text-primary focus:ring-ring"
                  />
                  <span className="ml-2 text-sm text-foreground">Visible on profile</span>
                </label>

                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={newExperience.isFeatured}
                    onChange={(e) => setNewExperience({...newExperience, isFeatured: e.target.checked})}
                    className="rounded border-input text-primary focus:ring-ring"
                  />
                  <span className="ml-2 text-sm text-foreground">Featured</span>
                </label>
              </div>
            </div>

            <div className="flex justify-end space-x-3 mt-6">
              <button
                onClick={() => setIsAddingExperience(false)}
                className="px-4 py-2 text-sm font-medium text-foreground bg-muted hover:bg-muted/80 rounded-full"
              >
                Cancel
              </button>
              <button
                onClick={handleAddExperience}
                disabled={!newExperience.title || !newExperience.organization || !newExperience.startDate}
                className="px-4 py-2 text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 disabled:bg-muted disabled:text-muted-foreground rounded-full"
              >
                Add Experience
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Edit Experience Modal - Similar to Add Modal but with editing experience data */}
      {editingExperience && (
        <div className="fixed inset-0 bg-background/80 overflow-y-auto h-full w-full z-50">
          <div className="relative top-10 mx-auto p-5 border border-border max-w-2xl shadow-lg rounded-md bg-card">
            <h3 className="text-lg font-bold text-foreground mb-4">Edit {editingExperience.title}</h3>
            
            <div className="space-y-4 max-h-96 overflow-y-auto">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    Type *
                  </label>
                  <select
                    value={editingExperience.type}
                    onChange={(e) => setEditingExperience({...editingExperience, type: e.target.value as ExperienceType})}
                    className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  >
                    <option value="Work">Work</option>
                    <option value="Education">Education</option>
                    <option value="Project">Project</option>
                    <option value="Volunteer">Volunteer</option>
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    Title *
                  </label>
                  <input
                    type="text"
                    value={editingExperience.title}
                    onChange={(e) => setEditingExperience({...editingExperience, title: e.target.value})}
                    className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    Organization *
                  </label>
                  <input
                    type="text"
                    value={editingExperience.organization}
                    onChange={(e) => setEditingExperience({...editingExperience, organization: e.target.value})}
                    className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    Location
                  </label>
                  <input
                    type="text"
                    value={editingExperience.location || ''}
                    onChange={(e) => setEditingExperience({...editingExperience, location: e.target.value})}
                    className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-1">
                  Description
                </label>
                <textarea
                  value={editingExperience.description || ''}
                  onChange={(e) => setEditingExperience({...editingExperience, description: e.target.value})}
                  className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  rows={4}
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    Start Date *
                  </label>
                  {/* BUG-HIGH-009 FIX: Use proper date formatting */}
                  <input
                    type="date"
                    value={new Date(editingExperience.startDate).toLocaleDateString('en-CA')}
                    onChange={(e) => setEditingExperience({...editingExperience, startDate: e.target.value})}
                    className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">
                    End Date
                  </label>
                  {/* BUG-HIGH-009 FIX: Use proper date formatting */}
                  <input
                    type="date"
                    value={editingExperience.endDate ? new Date(editingExperience.endDate).toLocaleDateString('en-CA') : ''}
                    onChange={(e) => setEditingExperience({...editingExperience, endDate: e.target.value, isCurrent: !e.target.value})}
                    className="w-full px-3 py-2 border border-input rounded-md focus:ring-ring focus:border-ring bg-background text-foreground"
                    disabled={editingExperience.isCurrent}
                  />
                </div>
              </div>

              <div className="flex items-center space-x-6">
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={editingExperience.isCurrent}
                    onChange={(e) => setEditingExperience({...editingExperience, isCurrent: e.target.checked, endDate: e.target.checked ? undefined : editingExperience.endDate})}
                    className="rounded border-input text-primary focus:ring-ring"
                  />
                  <span className="ml-2 text-sm text-foreground">Currently active</span>
                </label>

                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={editingExperience.isVisible}
                    onChange={(e) => setEditingExperience({...editingExperience, isVisible: e.target.checked})}
                    className="rounded border-input text-primary focus:ring-ring"
                  />
                  <span className="ml-2 text-sm text-foreground">Visible on profile</span>
                </label>

                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={editingExperience.isFeatured}
                    onChange={(e) => setEditingExperience({...editingExperience, isFeatured: e.target.checked})}
                    className="rounded border-input text-primary focus:ring-ring"
                  />
                  <span className="ml-2 text-sm text-foreground">Featured</span>
                </label>
              </div>
            </div>

            <div className="flex justify-end space-x-3 mt-6">
              <button
                onClick={() => setEditingExperience(null)}
                className="px-4 py-2 text-sm font-medium text-foreground bg-muted hover:bg-muted/80 rounded-full"
              >
                Cancel
              </button>
              <button
                onClick={() => handleUpdateExperience(editingExperience)}
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