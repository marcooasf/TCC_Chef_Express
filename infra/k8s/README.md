# Kubernetes — a criar (pós-MVP)

Estrutura planejada (Kustomize), para rodar localmente com **kind** ou **minikube**:

```
infra/k8s/
├── base/
│   ├── namespace.yaml
│   ├── mariadb/        # StatefulSet + Service + PVC + Secret
│   ├── api/            # Deployment + Service + ConfigMap + Secret
│   └── web/            # Deployment + Service (nginx com o build do Angular)
│   └── ingress.yaml    # /api -> api, / -> web
└── overlays/
    └── local/          # kustomization.yaml para o cluster local
```

A API já está preparada para isso:

- Configuração 100% por variáveis de ambiente (`ConnectionStrings__Default`, `Cors__AllowedOrigins__0`, ...).
- Probes: `GET /health/live` (liveness) e `GET /health/ready` (readiness, verifica o MariaDB).
- Imagem Docker sem root, porta `8080`.
