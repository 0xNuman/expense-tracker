import { useState } from 'react';
import { halFetch } from '../../hal/api';
import { useAuth } from '../../auth/AuthContext';

export function CsvImportWizard({ accountId }: { accountId: string }) {
  const { accessToken: token } = useAuth();
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<any>(null);
  const [mapping, setMapping] = useState({ occurredOnCol: 0, amountCol: 1, typeCol: 2, memoCol: 3 });
  const [importResult, setImportResult] = useState<any>(null);

  const handleUpload = async () => {
    if (!file) return;
    const base64 = await toBase64(file);
    if (!token) throw new Error('Not authenticated');
    const res = await halFetch<any>(`/api/accounts/${accountId}/import/preview`, { method: 'POST', body: JSON.stringify({ csvBase64: base64 }) }, token);
    setPreview(res);
  };

  const handleImport = async () => {
    if (!file) return;
    const base64 = await toBase64(file);
    if (!token) throw new Error('Not authenticated');
    const res = await halFetch<any>(`/api/accounts/${accountId}/import`, { 
        method: 'POST',
        body: JSON.stringify({ csvBase64: base64, mapping })
    }, token);
    setImportResult(res);
  };

  return (
    <div className="p-4 border rounded shadow bg-white">
      <h2 className="text-xl font-bold mb-4">CSV Import</h2>
      
      {!importResult ? (
        <>
          <div className="mb-4">
            <input type="file" accept=".csv" onChange={e => setFile(e.target.files?.[0] || null)} />
            <button className="bg-blue-500 text-white px-4 py-2 mt-2 rounded block" onClick={handleUpload}>Preview</button>
          </div>
          
          {preview && (
            <div className="mt-4">
              <h3 className="font-bold">Preview (First 10 Rows)</h3>
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm mt-2 table-auto border-collapse border border-slate-300">
                  <thead className="bg-slate-100">
                    <tr>{preview.headers.map((h: string, i: number) => <th key={i} className="border border-slate-300 px-2 py-1">{h}</th>)}</tr>
                  </thead>
                  <tbody>
                    {preview.rows.map((r: any) => (
                      <tr key={r.index}>
                        {r.values.map((v: string, i: number) => <td key={i} className="border border-slate-300 px-2 py-1">{v}</td>)}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              
              <div className="mt-4">
                <h3 className="font-bold mb-2">Column Mapping</h3>
                <div className="flex gap-4 mb-4">
                  <div>
                    <label className="block text-sm">Occurred On Col</label>
                    <input type="number" className="border px-2 py-1 w-20" value={mapping.occurredOnCol} onChange={e => setMapping({...mapping, occurredOnCol: parseInt(e.target.value)})} />
                  </div>
                  <div>
                    <label className="block text-sm">Amount Col</label>
                    <input type="number" className="border px-2 py-1 w-20" value={mapping.amountCol} onChange={e => setMapping({...mapping, amountCol: parseInt(e.target.value)})} />
                  </div>
                  <div>
                    <label className="block text-sm">Type Col</label>
                    <input type="number" className="border px-2 py-1 w-20" value={mapping.typeCol} onChange={e => setMapping({...mapping, typeCol: parseInt(e.target.value)})} />
                  </div>
                  <div>
                    <label className="block text-sm">Memo Col</label>
                    <input type="number" className="border px-2 py-1 w-20" value={mapping.memoCol} onChange={e => setMapping({...mapping, memoCol: parseInt(e.target.value)})} />
                  </div>
                </div>
                
                <button className="bg-green-500 text-white px-4 py-2 rounded" onClick={handleImport}>Confirm Import</button>
              </div>
            </div>
          )}
        </>
      ) : (
        <div className="p-4 bg-green-100 text-green-800 rounded">
          <h3 className="font-bold">Import Successful</h3>
          <ul className="list-disc pl-4 mt-2">
            <li>Total Imported: {importResult.totalImported}</li>
            <li>Duplicates Voided/Tagged: {importResult.voidedDuplicates}</li>
            <li>Errors: {importResult.errors}</li>
          </ul>
        </div>
      )}
    </div>
  );
}

function toBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.readAsDataURL(file);
    reader.onload = () => resolve((reader.result as string).split(',')[1]);
    reader.onerror = error => reject(error);
  });
}
