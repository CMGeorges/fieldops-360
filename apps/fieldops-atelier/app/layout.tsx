import type {Metadata} from 'next';
import './globals.css';
export const metadata:Metadata={title:'FieldOps Atelier — mon chantier, mes heures',description:'Mes dossiers de chantier, mes photos, mes heures et mes brouillons de devis.',manifest:'/manifest.webmanifest',icons:{icon:'/icon.svg'}};
export default function Layout({children}:{children:React.ReactNode}){return <html lang="fr"><body>{children}</body></html>;}
