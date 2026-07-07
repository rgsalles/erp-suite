import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

import { ErpApiService } from '../core/erp-api.service';
import { BusinessPartner, Material, PurchaseOrder, SalesOrder, Warehouse } from '../core/models';

type OrderMode = 'purchasing' | 'sales';
type AnyOrder = PurchaseOrder | SalesOrder;

@Component({
  selector: 'app-orders',
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe],
  templateUrl: './orders.component.html'
})
export class OrdersComponent implements OnInit {
  private readonly api = inject(ErpApiService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  readonly mode = signal<OrderMode>('purchasing');
  readonly orders = signal<AnyOrder[]>([]);
  readonly partners = signal<BusinessPartner[]>([]);
  readonly materials = signal<Material[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly error = signal('');

  readonly title = computed(() => this.mode() === 'purchasing' ? 'Pedidos de compra' : 'Pedidos de venda');
  readonly partnerLabel = computed(() => this.mode() === 'purchasing' ? 'Fornecedor' : 'Cliente');
  readonly priceLabel = computed(() => this.mode() === 'purchasing' ? 'Custo unitario' : 'Preco unitario');

  readonly form = this.fb.nonNullable.group({
    partnerId: ['', Validators.required],
    materialId: ['', Validators.required],
    quantity: [1, [Validators.required, Validators.min(0.001)]],
    price: [0, [Validators.required, Validators.min(0)]],
    warehouseId: ['', Validators.required],
    notes: ['']
  });

  ngOnInit() {
    const mode = this.route.snapshot.data['mode'] as OrderMode | undefined;
    this.mode.set(mode ?? 'purchasing');
    this.load();
  }

  load() {
    if (this.mode() === 'purchasing') {
      this.api.purchaseOrders().subscribe((orders) => this.orders.set(orders));
    } else {
      this.api.salesOrders().subscribe((orders) => this.orders.set(orders));
    }

    this.api.materials().subscribe((materials) => {
      this.materials.set(materials);
      if (!this.form.value.materialId && materials[0]) {
        this.form.patchValue({
          materialId: materials[0].id,
          price: this.mode() === 'purchasing' ? materials[0].standardCost : materials[0].salePrice
        });
      }
    });
    this.api.warehouses().subscribe((warehouses) => {
      this.warehouses.set(warehouses);
      if (!this.form.value.warehouseId && warehouses[0]) {
        this.form.patchValue({ warehouseId: warehouses[0].id });
      }
    });

    const partners = this.mode() === 'purchasing' ? this.api.suppliers() : this.api.customers();
    partners.subscribe((items) => {
      this.partners.set(items);
      if (!this.form.value.partnerId && items[0]) {
        this.form.patchValue({ partnerId: items[0].id });
      }
    });
  }

  create() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const item = {
      materialId: value.materialId,
      quantity: Number(value.quantity),
      unitCost: Number(value.price),
      unitPrice: Number(value.price)
    };

    if (this.mode() === 'purchasing') {
      this.api.createPurchaseOrder({
        supplierId: value.partnerId,
        notes: value.notes || null,
        items: [{ materialId: item.materialId, quantity: item.quantity, unitCost: item.unitCost }]
      }).subscribe({
        next: () => this.afterSave(),
        error: () => this.error.set('Nao foi possivel criar o pedido.')
      });
      return;
    }

    this.api.createSalesOrder({
        customerId: value.partnerId,
        notes: value.notes || null,
        items: [{ materialId: item.materialId, quantity: item.quantity, unitPrice: item.unitPrice }]
      }).subscribe({
      next: () => this.afterSave(),
      error: () => this.error.set('Nao foi possivel criar o pedido.')
    });
  }

  complete(order: AnyOrder) {
    const warehouseId = this.form.getRawValue().warehouseId;
    if (this.mode() === 'purchasing') {
      this.api.receivePurchaseOrder(order.id, warehouseId).subscribe({
        next: () => this.load(),
        error: () => this.error.set('Nao foi possivel concluir o pedido.')
      });
      return;
    }

    this.api.shipSalesOrder(order.id, warehouseId).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Nao foi possivel concluir o pedido.')
    });
  }

  afterSave() {
    this.form.patchValue({ quantity: 1, notes: '' });
    this.load();
  }

  partnerName(order: AnyOrder) {
    return 'supplierName' in order ? order.supplierName : order.customerName;
  }
}
