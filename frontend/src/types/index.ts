export type Rota = 'Quitandinha' | 'Bingen';

export interface Participante {
  id: string;
  nome: string;
  whatsApp: string;
  grupoId: string;
}

export interface Grupo {
  id: string;
  nome: string;
  rota: Rota;
  participantes: Participante[];
}

export interface Jogo {
  id: string;
  competicaoCodigo: string;
  adversario: string;
  data: string;
  horarioSaida: string;
  pontoEmbarque: string;
}

export interface Pagamento {
  id: string;
  participanteId: string;
  jogoId: string;
  pago: boolean;
}

export interface DesmarcarPagamento {
  reembolsoDevido: boolean;
}

export interface VanAlocada {
  id: string;
  motorista: string;
  responsavelId: string;
  grupoIds: string[];
  ocupacao: number;
}

export interface SpinAlocado {
  id: string;
  grupoIds: string[];
  ocupacao: number;
}

export interface Alocacao {
  id: string;
  jogoId: string;
  rota: Rota;
  vans: VanAlocada[];
  spin?: SpinAlocado | null;
  gruposEscalados: string[];
  gruposPendentes: string[];
}

export interface ListaOficial {
  texto: string;
}

export interface LoginResponse {
  token: string;
}

export interface GrupoRequest {
  nome: string;
  rota: Rota;
}

export interface ParticipanteRequest {
  nome: string;
  whatsApp: string;
  grupoId: string;
}

export interface JogoRequest {
  competicaoCodigo: string;
  adversario: string;
  data: string;
  horarioSaida: string;
  pontoEmbarque: string;
}

export interface DefinirResponsavelVanRequest {
  motorista: string;
  responsavelId: string;
}
