import React, { useState, useEffect } from 'react';
import { useAuth } from '../../auth/AuthContext';
import { FolderTree, Plus, Edit2, Archive, RefreshCw, ChevronRight, ChevronDown, CheckCircle } from 'lucide-react';

// API Client Types
export interface Category {
    id: string;
    parentId?: string;
    name: string;
    kind: 'Income' | 'Expense' | 'Either';
    icon?: string;
    color?: string;
    sortOrder: number;
    isArchived: boolean;
}

export const fetchCategories = async (token: string, includeArchived: boolean = false): Promise<Category[]> => {
    const res = await fetch(`/api/categories?includeArchived=${includeArchived}`, {
        headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!res.ok) throw new Error('Failed to fetch categories');
    const data = await res.json();
    return data._embedded?.categories || [];
};

export const createCategory = async (token: string, payload: any): Promise<Category> => {
    const res = await fetch('/api/categories', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error('Failed to create category');
    return res.json();
};

export const updateCategory = async (token: string, id: string, payload: any): Promise<Category> => {
    const res = await fetch(`/api/categories/${id}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error('Failed to update category');
    return res.json();
};

export const archiveCategory = async (token: string, id: string): Promise<Category> => {
    const res = await fetch(`/api/categories/${id}/archive`, { 
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!res.ok) throw new Error('Failed to archive');
    return res.json();
};

export const restoreCategory = async (token: string, id: string): Promise<Category> => {
    const res = await fetch(`/api/categories/${id}/restore`, { 
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!res.ok) throw new Error('Failed to restore');
    return res.json();
};

export const CategoriesTree: React.FC = () => {
    const { accessToken } = useAuth();
    const [categories, setCategories] = useState<Category[]>([]);
    const [showArchived, setShowArchived] = useState(false);
    
    // Modal states
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [modalMode, setModalMode] = useState<'create' | 'edit'>('create');
    const [editingId, setEditingId] = useState<string | null>(null);
    const [formData, setFormData] = useState({ name: '', kind: 'Expense', parentId: '' });
    
    // UI states
    const [expandedNodes, setExpandedNodes] = useState<Record<string, boolean>>({});

    useEffect(() => {
        if (accessToken) {
            fetchCategories(accessToken, showArchived).then(setCategories).catch(console.error);
        }
    }, [showArchived, accessToken]);

    const toggleNode = (id: string) => {
        setExpandedNodes(prev => ({ ...prev, [id]: !prev[id] }));
    };

    const openCreateModal = (parentId?: string) => {
        setModalMode('create');
        setFormData({ name: '', kind: 'Expense', parentId: parentId || '' });
        setIsModalOpen(true);
    };

    const openEditModal = (cat: Category) => {
        setModalMode('edit');
        setEditingId(cat.id);
        setFormData({ name: cat.name, kind: cat.kind, parentId: cat.parentId || '' });
        setIsModalOpen(true);
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!accessToken) return;
        
        try {
            const payload = { 
                name: formData.name, 
                kind: formData.kind, 
                parentId: formData.parentId || null 
            };

            if (modalMode === 'create') {
                const newCat = await createCategory(accessToken, payload);
                setCategories([...categories, newCat]);
                if (payload.parentId) {
                    setExpandedNodes(prev => ({ ...prev, [payload.parentId as string]: true }));
                }
            } else if (modalMode === 'edit' && editingId) {
                const updatedCat = await updateCategory(accessToken, editingId, payload);
                setCategories(categories.map(c => c.id === editingId ? updatedCat : c));
            }
            setIsModalOpen(false);
        } catch (e) {
            console.error(e);
            alert('Failed to save category. Please ensure names are unique and valid.');
        }
    };

    const handleArchiveToggle = async (cat: Category) => {
        if (!accessToken) return;
        try {
            if (cat.isArchived) {
                const restored = await restoreCategory(accessToken, cat.id);
                setCategories(categories.map(c => c.id === cat.id ? restored : c));
            } else {
                const archived = await archiveCategory(accessToken, cat.id);
                setCategories(categories.map(c => c.id === cat.id ? archived : c));
            }
        } catch (e) {
            console.error(e);
        }
    };

    const renderTree = (parentId?: string, depth = 0) => {
        const children = categories
            .filter(c => (parentId ? c.parentId === parentId : !c.parentId))
            .sort((a, b) => a.sortOrder - b.sortOrder);

        return children.map(c => {
            const hasChildren = categories.some(child => child.parentId === c.id);
            const isExpanded = expandedNodes[c.id] !== false; // default expanded

            return (
                <div key={c.id} className="w-full">
                    <div 
                        className={`flex items-center justify-between group rounded-xl p-3 my-1 transition-all duration-200 border border-transparent
                            ${depth === 0 ? 'bg-white dark:bg-gray-800 shadow-sm hover:shadow-md' : 'hover:bg-gray-50 dark:hover:bg-gray-700'} 
                            ${c.isArchived ? 'opacity-60 grayscale' : ''}
                        `}
                        style={{ marginLeft: depth > 0 ? '1.5rem' : '0' }}
                    >
                        <div className="flex items-center gap-3">
                            {hasChildren ? (
                                <button onClick={() => toggleNode(c.id)} className="p-1 rounded-md hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors">
                                    {isExpanded ? <ChevronDown size={18} className="text-gray-500" /> : <ChevronRight size={18} className="text-gray-500" />}
                                </button>
                            ) : (
                                <div className="w-[26px]" /> // Spacer
                            )}
                            
                            <div className={`p-2 rounded-lg ${c.kind === 'Income' ? 'bg-emerald-100 text-emerald-600 dark:bg-emerald-900/30' : c.kind === 'Expense' ? 'bg-rose-100 text-rose-600 dark:bg-rose-900/30' : 'bg-blue-100 text-blue-600 dark:bg-blue-900/30'}`}>
                                <FolderTree size={20} />
                            </div>
                            
                            <div className="flex flex-col">
                                <span className="font-semibold text-gray-900 dark:text-white text-base tracking-tight">{c.name}</span>
                                <span className="text-xs font-medium text-gray-500 uppercase tracking-wider">{c.kind}</span>
                            </div>
                        </div>

                        <div className="flex items-center gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                            <button onClick={() => openCreateModal(c.id)} className="p-2 text-gray-400 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/30 rounded-lg transition-colors tooltip" title="Add Child Category">
                                <Plus size={18} />
                            </button>
                            <button onClick={() => openEditModal(c)} className="p-2 text-gray-400 hover:text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-900/30 rounded-lg transition-colors" title="Edit">
                                <Edit2 size={18} />
                            </button>
                            <button onClick={() => handleArchiveToggle(c)} className={`p-2 rounded-lg transition-colors ${c.isArchived ? 'text-emerald-500 hover:bg-emerald-50 dark:hover:bg-emerald-900/30' : 'text-gray-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-900/30'}`} title={c.isArchived ? 'Restore' : 'Archive'}>
                                {c.isArchived ? <RefreshCw size={18} /> : <Archive size={18} />}
                            </button>
                        </div>
                    </div>

                    {/* Children Container with visual hierarchy line */}
                    {hasChildren && isExpanded && (
                        <div className="relative">
                            <div className="absolute left-6 top-0 bottom-6 w-px bg-gray-200 dark:bg-gray-700" />
                            {renderTree(c.id, depth + 1)}
                        </div>
                    )}
                </div>
            );
        });
    };

    return (
        <div className="max-w-4xl mx-auto p-4 sm:p-8">
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-8 gap-4">
                <div>
                    <h2 className="text-3xl font-extrabold text-gray-900 dark:text-white tracking-tight flex items-center gap-3">
                        Categories
                    </h2>
                    <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">Organize your finances with a flexible hierarchy.</p>
                </div>
                <div className="flex items-center gap-4">
                    <label className="flex items-center gap-2 cursor-pointer text-sm font-medium text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white transition-colors">
                        <input 
                            type="checkbox" 
                            className="w-4 h-4 rounded border-gray-300 text-blue-600 focus:ring-blue-600 cursor-pointer"
                            checked={showArchived} 
                            onChange={e => setShowArchived(e.target.checked)} 
                        />
                        Show Archived
                    </label>
                    <button 
                        onClick={() => openCreateModal()} 
                        className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white px-5 py-2.5 rounded-xl font-medium shadow-sm hover:shadow-md transition-all active:scale-95"
                    >
                        <Plus size={18} />
                        New Category
                    </button>
                </div>
            </div>

            <div className="bg-gray-50 dark:bg-gray-800/50 rounded-2xl p-4 sm:p-6 border border-gray-100 dark:border-gray-700/50 min-h-[500px]">
                {categories.length === 0 ? (
                    <div className="flex flex-col items-center justify-center h-[400px] text-gray-400">
                        <FolderTree size={48} className="mb-4 opacity-50" />
                        <p className="text-lg font-medium">No categories found.</p>
                        <p className="text-sm">Create one to get started.</p>
                    </div>
                ) : (
                    <div className="flex flex-col">
                        {renderTree()}
                    </div>
                )}
            </div>

            {/* Modal */}
            {isModalOpen && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-gray-900/60 backdrop-blur-sm animate-in fade-in duration-200">
                    <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl w-full max-w-md overflow-hidden transform transition-all">
                        <div className="px-6 py-4 border-b border-gray-100 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50">
                            <h3 className="text-xl font-bold text-gray-900 dark:text-white">
                                {modalMode === 'create' ? 'Create Category' : 'Edit Category'}
                            </h3>
                        </div>
                        <form onSubmit={handleSubmit} className="p-6">
                            <div className="space-y-5">
                                <div>
                                    <label className="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-1">Name</label>
                                    <input 
                                        type="text" 
                                        required 
                                        maxLength={60}
                                        value={formData.name}
                                        onChange={e => setFormData({ ...formData, name: e.target.value })}
                                        className="w-full px-4 py-2.5 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-shadow outline-none"
                                        placeholder="e.g. Groceries"
                                    />
                                </div>
                                
                                <div>
                                    <label className="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-1">Type</label>
                                    <select 
                                        value={formData.kind}
                                        onChange={e => setFormData({ ...formData, kind: e.target.value as any })}
                                        className="w-full px-4 py-2.5 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                                    >
                                        <option value="Expense">Expense</option>
                                        <option value="Income">Income</option>
                                        <option value="Either">Either (Mixed)</option>
                                    </select>
                                </div>

                                {/* Parent Category Picker (simplified for now) */}
                                {modalMode === 'create' && (
                                    <div>
                                        <label className="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-1">Parent (Optional)</label>
                                        <select 
                                            value={formData.parentId}
                                            onChange={e => setFormData({ ...formData, parentId: e.target.value })}
                                            className="w-full px-4 py-2.5 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                                        >
                                            <option value="">-- None (Root Level) --</option>
                                            {categories.filter(c => !c.isArchived).map(c => (
                                                <option key={c.id} value={c.id}>{c.name}</option>
                                            ))}
                                        </select>
                                    </div>
                                )}
                            </div>

                            <div className="mt-8 flex items-center justify-end gap-3">
                                <button 
                                    type="button" 
                                    onClick={() => setIsModalOpen(false)}
                                    className="px-5 py-2.5 rounded-xl text-gray-600 dark:text-gray-300 font-medium hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                                >
                                    Cancel
                                </button>
                                <button 
                                    type="submit" 
                                    className="px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-medium shadow-md shadow-blue-500/20 transition-all active:scale-95 flex items-center gap-2"
                                >
                                    <CheckCircle size={18} />
                                    {modalMode === 'create' ? 'Create' : 'Save Changes'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
};

