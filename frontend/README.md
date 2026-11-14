# Front-end (Angular) — a criar

Reservado para o projeto Angular (tarefas 3 e 5 do backlog).

Quando for criado:

1. Gerar o projeto **dentro desta pasta** (ex.: `npx @angular/cli@latest new chefexpress-web --directory .`).
2. Apontar o endereço da API para `http://localhost:5094` (variável de ambiente do Angular).
3. Adicionar um `Dockerfile` (build com Node + servir com nginx) e descomentar o serviço `web` no `docker-compose.yml` da raiz.
4. Criar `.github/workflows/frontend-ci.yml` (lint + build + testes).

Contrato da API: acesse `http://localhost:5094/scalar` com a API rodando.
